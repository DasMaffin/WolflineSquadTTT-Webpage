using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text;
using System.Text.Json;

namespace WolflineSquadTTT.Infrastructure.Security
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequiresApiPrivateKeyAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private const string HeaderName = "X-Api-Key";
        private const string BodyKeyName = "apiPrivateKey";

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var http = context.HttpContext;

            var validKeys = http.RequestServices
                .GetRequiredService<IConfiguration>()
                .GetSection("ApiPrivateKeys")
                .Get<List<Guid>>() ?? new();

            if (validKeys.Count == 0)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            // Accept the key from the X-Api-Key header (works for GETs) or, as a fallback,
            // from an "apiPrivateKey" field in the JSON body (for POSTs that already send one).
            Guid? providedKey = ReadFromHeader(http.Request) ?? await ReadFromBodyAsync(http.Request);

            if (providedKey == null)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (!validKeys.Contains(providedKey.Value))
            {
                context.Result = new StatusCodeResult(403);
            }
        }

        private static Guid? ReadFromHeader(HttpRequest request)
        {
            if (request.Headers.TryGetValue(HeaderName, out var value) && Guid.TryParse(value.ToString(), out var key))
                return key;

            return null;
        }

        private static async Task<Guid?> ReadFromBodyAsync(HttpRequest request)
        {
            // Enable rereading so model binding still works after we peek at the body.
            request.EnableBuffering();

            string body;
            using (var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
            {
                body = await reader.ReadToEndAsync();
            }
            request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(body))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty(BodyKeyName, out var keyProp) && Guid.TryParse(keyProp.GetString(), out var key))
                    return key;
            }
            catch
            {
                // not JSON / no key present — treat as no key
            }

            return null;
        }
    }
}
