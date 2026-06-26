using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using WolflineSquadTTT.Models;
using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Services
{
    public interface IWebhookService
    {
        Task<List<Webhook>> GetAllAsync();
        Task AddAsync(string? name, string url, WebhookEvent evt, WebhookFormat format, string? createdBySteamId);
        Task UpdateAsync(int id, string? name, string url, WebhookEvent evt, WebhookFormat format);
        Task DeleteAsync(int id);
        Task DispatchAsync(WebhookEvent evt, WebhookMessage message);
    }

    // Stores configured webhooks and fires them. Each webhook chooses its target application
    // (Generic = raw text, Discord = a styled embed). Dispatch never throws — a bad webhook can't
    // break the caller.
    public class WebhookService : IWebhookService
    {
        private readonly AppDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _baseUrl;

        public WebhookService(AppDbContext db, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _baseUrl = (config["App:PublicBaseUrl"] ?? "https://mwlp.dasmaffin.com").TrimEnd('/');
        }

        public async Task<List<Webhook>> GetAllAsync()
        {
            return await _db.Webhook.OrderBy(w => w.Event).ThenByDescending(w => w.CreatedAt).ToListAsync();
        }

        public async Task AddAsync(string? name, string url, WebhookEvent evt, WebhookFormat format, string? createdBySteamId)
        {
            _db.Webhook.Add(new Webhook
            {
                Name = name ?? string.Empty,
                Url = url,
                Event = evt,
                Format = format,
                Enabled = true,
                CreatedBySteamId = createdBySteamId
            });
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(int id, string? name, string url, WebhookEvent evt, WebhookFormat format)
        {
            Webhook? hook = await _db.Webhook.FirstOrDefaultAsync(w => w.Id == id);
            if (hook == null)
                return;

            hook.Name = name ?? string.Empty;
            hook.Url = url;
            hook.Event = evt;
            hook.Format = format;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            Webhook? hook = await _db.Webhook.FirstOrDefaultAsync(w => w.Id == id);
            if (hook == null)
                return;

            _db.Webhook.Remove(hook);
            await _db.SaveChangesAsync();
        }

        public async Task DispatchAsync(WebhookEvent evt, WebhookMessage message)
        {
            List<Webhook> hooks;
            try
            {
                hooks = await _db.Webhook.Where(w => w.Enabled && w.Event == evt).ToListAsync();
            }
            catch
            {
                return;   // webhook subsystem must never break the caller
            }

            if (hooks.Count == 0)
                return;

            HttpClient client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(8);

            foreach (Webhook hook in hooks)
            {
                try
                {
                    using HttpContent body = hook.Format == WebhookFormat.Discord
                        ? new StringContent(BuildDiscordPayload(message), Encoding.UTF8, "application/json")
                        : new StringContent(BuildGenericText(message), Encoding.UTF8, "text/plain");
                    await client.PostAsync(hook.Url, body);
                }
                catch
                {
                    // A single failing/misconfigured webhook shouldn't affect the others or the caller.
                }
            }
        }

        private string? AbsoluteUrl(string? relative) =>
            string.IsNullOrEmpty(relative) ? null : $"{_baseUrl}/{relative.TrimStart('/')}";

        // Plain, unstyled text for arbitrary receivers — no markdown, just the facts.
        private string BuildGenericText(WebhookMessage m)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[').Append(m.Headline).Append("] ").AppendLine(m.Title);
            if (!string.IsNullOrWhiteSpace(m.Description))
                sb.AppendLine(m.Description);
            foreach (WebhookField f in m.Fields)
                sb.Append("- ").Append(f.Name).Append(": ").AppendLine(f.Value);
            string? abs = AbsoluteUrl(m.RelativeUrl);
            if (abs != null)
                sb.AppendLine(abs);
            return sb.ToString().TrimEnd();
        }

        // A pretty Discord message: header in the content line, the rest in a coloured embed, leaning
        // on Discord-flavoured markdown — headers (##), subtext (-#), a masked link and a live relative
        // timestamp (<t:..:R>), both of which only render in bot/webhook messages.
        private string BuildDiscordPayload(WebhookMessage m)
        {
            string? abs = AbsoluteUrl(m.RelativeUrl);
            long unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            List<string> descLines = new List<string>();
            if (!string.IsNullOrWhiteSpace(m.Description))
                descLines.Add($"> {m.Description.Replace("\n", "\n> ")}");      // blockquote
            if (abs != null)
                descLines.Add($"**[Open in the browser ↗]({abs})**");           // masked link (webhook-only flair)
            descLines.Add($"-# Posted <t:{unix}:R>");                            // subtext + live relative time

            var embed = new
            {
                title = m.Title,
                url = abs,
                description = string.Join("\n\n", descLines),
                color = m.Color,
                fields = m.Fields.Select(f => new
                {
                    name = f.Name,
                    value = $"`{f.Value}`",                                      // monospace value
                    inline = f.Inline
                }).ToArray(),
                footer = new { text = "WolflineSquad TTT" },
                timestamp = DateTimeOffset.UtcNow.ToString("o")
            };

            var payload = new
            {
                content = $"## {m.Emoji} {m.Headline}",                          // big header line
                embeds = new[] { embed }
            };

            return JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }
    }
}
