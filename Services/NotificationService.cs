using BukuTamuAPI.DTOs;
using BukuTamuAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BukuTamuAPI.Services;

public interface INotificationService
{
    Task<IEnumerable<NotificationResponse>> GetNotificationsForUser(int userId, bool? isRead = null, int limit = 10);
    Task CreateNotification(NotificationCreateRequest request);
    Task MarkAsRead(int id, int userId);
    Task DeleteNotification(int id, int userId);
}

public class NotificationService : INotificationService
{
    private readonly DbtamuContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(DbtamuContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CreateNotification(NotificationCreateRequest request)
    {
        var notification = new Notifikasi
        {
            IdPengguna = request.IdPengguna,
            Pesan = request.Pesan,
            Waktu = DateTime.Now,
            IsRead = false
        };

        _context.Notifikasis.Add(notification);
        await _context.SaveChangesAsync();
    }
    public async Task<IEnumerable<NotificationResponse>> GetNotificationsForUser(int userId, bool? isRead = null, int limit = 10)
    {
        IQueryable<Notifikasi> query = _context.Notifikasis
            .Where(n => n.IdPengguna == userId)
            .OrderByDescending(n => n.Waktu);

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        var notifications = await query
            .Take(limit)
            .Select(n => new NotificationResponse
            {
                IdNotifikasi = n.IdNotifikasi,
                Pesan = n.Pesan,
                Waktu = n.Waktu.Value, 
                IsRead = n.IsRead ?? false 
            })
            .ToListAsync();

        return notifications;
    }

    public async Task MarkAsRead(int id, int userId)
    {
        var notification = await _context.Notifikasis
            .FirstOrDefaultAsync(n => n.IdNotifikasi == id && n.IdPengguna == userId);

        if (notification == null)
        {
            throw new KeyNotFoundException("Notifikasi tidak ditemukan");
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteNotification(int id, int userId)
    {
        var notification = await _context.Notifikasis
            .FirstOrDefaultAsync(n => n.IdNotifikasi == id && n.IdPengguna == userId);

        if (notification == null)
        {
            throw new KeyNotFoundException("Notifikasi tidak ditemukan");
        }

        _context.Notifikasis.Remove(notification);
        await _context.SaveChangesAsync();
    }
}

