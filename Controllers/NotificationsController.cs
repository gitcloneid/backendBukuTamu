using System.Security.Claims;
using BukuTamuAPI.DTOs;
using BukuTamuAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BukuTamuAPI.Controllers;

[Route("api/notifications")]
[ApiController]
[Authorize]

public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(INotificationService notificationService, ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationResponse>>> GetNotifications(
        [FromQuery] bool? read,
        [FromQuery] int limit = 10)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var notifications = await _notificationService.GetNotificationsForUser(userId, read, limit);
            return Ok(new { data = notifications });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat mengambil notifikasi");
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkNotificationAsRead(int id)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            await _notificationService.MarkAsRead(id, userId);
            return Ok(new { message = "Notifikasi berhasil ditandai sebagai dibaca" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Notifikasi dengan ID {Id} tidak ditemukan", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat menandai notifikasi dengan ID {Id} sebagai dibaca", id);
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            await _notificationService.DeleteNotification(id, userId);
            return Ok(new { message = "Notifikasi berhasil dihapus" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Notifikasi dengan ID {Id} tidak ditemukan", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat menghapus notifikasi dengan ID {Id}", id);
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }
}