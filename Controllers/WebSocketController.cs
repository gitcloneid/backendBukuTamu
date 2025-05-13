using BukuTamuAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace BukuTamuAPI.Controllers;

[Route("api/ws")]
[ApiController]
public class WebSocketController : ControllerBase
{
    private readonly WebSocketHandler _webSocketHandler;

    public WebSocketController(WebSocketHandler webSocketHandler)
    {
        _webSocketHandler = webSocketHandler;
    }

    [HttpGet]
    public async Task Get()
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await _webSocketHandler.HandleConnection(webSocket);
        }
        else
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}