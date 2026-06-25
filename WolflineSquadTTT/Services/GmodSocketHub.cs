using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace WolflineSquadTTT.Services
{
    public interface IGmodSocketHub
    {
        bool HasActiveConnection { get; }
        Task HandleConnectionAsync(WebSocket socket, CancellationToken cancellationToken);
        Task NotifyReloadInventoryAsync(string steamId);
    }

    /// <summary>
    /// Holds the WebSocket connections the GMod server opens to us and pushes hook messages back to them
    /// (e.g. telling the server to reload a player's Pointshop inventory after the website changed the DB).
    /// Singleton so the live connections outlive any single request.
    /// </summary>
    public class GmodSocketHub : IGmodSocketHub
    {
        private readonly ConcurrentDictionary<Guid, WebSocket> _sockets = new();
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        public bool HasActiveConnection => _sockets.Values.Any(socket => socket.State == WebSocketState.Open);

        public async Task HandleConnectionAsync(WebSocket socket, CancellationToken cancellationToken)
        {
            Guid id = Guid.NewGuid();
            _sockets[id] = socket;

            try
            {
                // We don't need anything the server sends; just hold the socket open until it closes.
                byte[] buffer = new byte[1024];
                while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException) { }
            finally
            {
                _sockets.TryRemove(id, out _);
            }
        }

        public async Task NotifyReloadInventoryAsync(string steamId)
        {
            string payload = JsonSerializer.Serialize(new
            {
                hook = "MaffinAPI_PointshopReloadInventory",
                args = new[] { steamId }
            });
            byte[] bytes = Encoding.UTF8.GetBytes(payload);

            await _sendLock.WaitAsync();
            try
            {
                foreach (KeyValuePair<Guid, WebSocket> entry in _sockets)
                {
                    if (entry.Value.State != WebSocketState.Open)
                    {
                        _sockets.TryRemove(entry.Key, out _);
                        continue;
                    }

                    try
                    {
                        await entry.Value.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch
                    {
                        _sockets.TryRemove(entry.Key, out _);
                    }
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }
    }
}
