using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using WolflineSquadTTT.Services;

namespace WolflineSquadTTT.Controllers
{
    [Route("ws/gmod")]
    public class GmodSocketController : ControllerBase
    {
        private readonly IGmodSocketHub _hub;
        private readonly List<Guid> _apiKeys;

        public GmodSocketController(IGmodSocketHub hub, IConfiguration config)
        {
            _hub = hub;
            _apiKeys = config.GetSection("ApiPrivateKeys").Get<List<string>>()
                ?.Select(k => Guid.TryParse(k, out Guid g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .ToList() ?? new List<Guid>();
        }

        // The GMod server opens this WebSocket (authenticated by API key) and we hold it open to push hooks.
        [HttpGet]
        public async Task Get()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            if (!IsAuthorized())
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            using WebSocket socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await _hub.HandleConnectionAsync(socket, HttpContext.RequestAborted);
        }

        private bool IsAuthorized()
        {
            if (_apiKeys.Count == 0)
                return false;

            string? provided = Request.Headers["X-Api-Key"].FirstOrDefault()
                ?? Request.Query["apiKey"].FirstOrDefault();

            return Guid.TryParse(provided, out Guid key) && _apiKeys.Contains(key);
        }
    }
}
