using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text;
using System.Text.Json;
using WolflineSquadTTT.Models.Enums;

namespace WolflineSquadTTT.Infrastructure.Security
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RequiresApiPrivateKeyAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private const string KeyName = "apiPrivateKey";
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var http = context.HttpContext;

            // Read valid keys from configuration
            var config = http.RequestServices.GetRequiredService<IConfiguration>();
            var validKeys = config
                .GetSection("ApiPrivateKeys")
                .Get<List<Guid>>() ?? new();

            if (validKeys.Count == 0)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }

            // Enable rereading the body
            http.Request.EnableBuffering();

            string body;

            using (var reader = new StreamReader(
                http.Request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true))
            {
                body = await reader.ReadToEndAsync();
            }

            // Reset stream position so model binding still works
            http.Request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(body))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            Guid providedKey;

            try
            {
                using var doc = JsonDocument.Parse(body);

                if (!doc.RootElement.TryGetProperty(KeyName, out var keyProp))
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }

                if (!Guid.TryParse(keyProp.GetString(), out providedKey))
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }
            }
            catch
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Validate key
            if (!validKeys.Contains(providedKey))
            {
                context.Result = new ForbidResult();
                return;
            }

            // Authorized — do nothing
        }
    }
}
