namespace BukuTamuAPI.Services;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;



public class WebSocketHandler
{
    private readonly List<WebSocket> _sockets = new();
    private readonly ILogger<WebSocketHandler> _logger;

    public WebSocketHandler(ILogger<WebSocketHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleConnection(WebSocket webSocket)
    {
        _sockets.Add(webSocket);
        var buffer = new byte[1024 * 4];
        
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebSocket error");
        }
        finally
        {
            _sockets.Remove(webSocket);
            webSocket.Dispose();
        }
    }

    public async Task BroadcastNotification(int userId, string message)
    {
        var notification = new 
        {
            userId,
            message,
            timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(notification);
        var buffer = Encoding.UTF8.GetBytes(json);
        
        foreach (var socket in _sockets.Where(s => s.State == WebSocketState.Open))
        {
            try
            {
                await socket.SendAsync(
                    new ArraySegment<byte>(buffer, 0, buffer.Length),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send WebSocket message");
            }
        }
    }
}