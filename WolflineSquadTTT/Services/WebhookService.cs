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
        Task AddAsync(string? name, string url, WebhookEvent evt, string? createdBySteamId);
        Task DeleteAsync(int id);
        Task DispatchAsync(WebhookEvent evt, string content);
    }

    // Stores configured webhooks and fires them. Payload is Discord-compatible (`{ "content": "..." }`),
    // which most webhook receivers accept. Dispatch never throws — a bad webhook can't break the caller.
    public class WebhookService : IWebhookService
    {
        private readonly AppDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;

        public WebhookService(AppDbContext db, IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<Webhook>> GetAllAsync()
        {
            return await _db.Webhook.OrderBy(w => w.Event).ThenByDescending(w => w.CreatedAt).ToListAsync();
        }

        public async Task AddAsync(string? name, string url, WebhookEvent evt, string? createdBySteamId)
        {
            _db.Webhook.Add(new Webhook
            {
                Name = name ?? string.Empty,
                Url = url,
                Event = evt,
                Enabled = true,
                CreatedBySteamId = createdBySteamId
            });
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

        public async Task DispatchAsync(WebhookEvent evt, string content)
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

            string json = JsonSerializer.Serialize(new { content });

            HttpClient client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(8);

            foreach (Webhook hook in hooks)
            {
                try
                {
                    using StringContent body = new StringContent(json, Encoding.UTF8, "application/json");
                    await client.PostAsync(hook.Url, body);
                }
                catch
                {
                    // A single failing/misconfigured webhook shouldn't affect the others or the caller.
                }
            }
        }
    }
}
