using BukuTamuAPI.DTOs;
using BukuTamuAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BukuTamuAPI.Services;


public interface IAppointmentService
{
    Task<(IEnumerable<AppointmentResponse> appointments, int totalCount)> GetAllAppointments(
        DateOnly? date, string? status, int page, int limit);
    Task<IEnumerable<AppointmentResponse>> GetAppointmentsByGuru(int idGuru, DateOnly? date, string? status);
    Task<AppointmentResponse> CreateAppointment(AppointmentCreateRequest request, int idGuru);
    Task<AppointmentResponse> UpdateAppointmentStatus(int id, string status, WebSocketHandler webSocketHandler);
    Task<AppointmentResponse> VerifyQrCode(string kode);
    Task<IEnumerable<AppointmentResponse>> GetTodayAppointments();
}
public class AppointmentService : IAppointmentService
{
    private readonly DbtamuContext _context;
    private readonly ILogger<AppointmentService> _logger;
    private readonly INotificationService _notificationService;

    public AppointmentService(
        DbtamuContext context,
        ILogger<AppointmentService> logger,
        INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<(IEnumerable<AppointmentResponse>, int)> GetAllAppointments(
        DateOnly? date, string? status, int page = 1, int limit = 10)
    {
        var query = _context.JanjiTemus
            .Include(j => j.IdTamuNavigation)
            .Include(j => j.IdGuruNavigation)
            .AsQueryable();

        if (date.HasValue)
        {
            query = query.Where(j => j.Tanggal == date.Value);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(j => j.Status == status);
        }

        var totalCount = await query.CountAsync();

        var appointments = await query
            .OrderBy(j => j.Tanggal)
            .ThenBy(j => j.Waktu)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(j => new AppointmentResponse
            {
                IdJanjiTemu = j.IdJanjiTemu,
                Tanggal = j.Tanggal.ToString("yyyy-MM-dd"),
                Waktu = j.Waktu.ToString("HH:mm"),
                Status = j.Status,
                Keperluan = j.Keperluan,
                KodeQr = j.KodeQr,
                Tamu = new TamuResponse
                {
                    IdTamu = j.IdTamuNavigation.IdTamu,
                    Nama = j.IdTamuNavigation.Nama,
                    Telepon = j.IdTamuNavigation.Telepon
                },
                Guru = new UserResponse
                {
                    IdPengguna = j.IdGuruNavigation.IdPengguna,
                    Nama = j.IdGuruNavigation.Nama
                }
            })
            .ToListAsync();

        return (appointments, totalCount);
    }

    public async Task<IEnumerable<AppointmentResponse>> GetAppointmentsByGuru(int idGuru, DateOnly? date, string? status)
    {
        var query = _context.JanjiTemus
            .Include(j => j.IdTamuNavigation)
            .Where(j => j.IdGuru == idGuru);

        if (date.HasValue)
        {
            query = query.Where(j => j.Tanggal == date.Value);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(j => j.Status == status);
        }

        return await query
            .OrderBy(j => j.Tanggal)
            .ThenBy(j => j.Waktu)
            .Select(j => new AppointmentResponse
            {
                IdJanjiTemu = j.IdJanjiTemu,
                Tanggal = j.Tanggal.ToString("yyyy-MM-dd"),
                Waktu = j.Waktu.ToString("HH:mm"),
                Status = j.Status,
                Keperluan = j.Keperluan,
                Tamu = new TamuResponse
                {
                    IdTamu = j.IdTamuNavigation.IdTamu,
                    Nama = j.IdTamuNavigation.Nama,
                    Telepon = j.IdTamuNavigation.Telepon
                }
            })
            .ToListAsync();
    }

    public async Task<AppointmentResponse> CreateAppointment(AppointmentCreateRequest request, int idGuru)
    {
        //datetime yyyy-MM-dd
        if (!DateOnly.TryParseExact(request.Tanggal, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var tanggal))
        {
            throw new ArgumentException("Tanggal must be in yyyy-MM-dd format");
        }
        
        //parse waktu
        if (!TimeOnly.TryParseExact(request.Waktu, "HH:mm", null, System.Globalization.DateTimeStyles.None, out var waktu))
        {
            throw new ArgumentException("Waktu must be in HH:mm format");
        }
        
        var tamu = await _context.Tamus.FindAsync(request.IdTamu);
        if (tamu == null)
        {
            throw new ArgumentException($"Tamu with ID {request.IdTamu} not found");
        }
        
        var guru = await _context.Penggunas.FindAsync(idGuru);
        if (guru == null || guru.Role != "Guru")
        {
            throw new ArgumentException($"Guru with ID {idGuru} not found or not a valid teacher");
        }

        var appointment = new JanjiTemu
        {
            IdTamu = request.IdTamu,
            IdGuru = idGuru,
            Tanggal = tanggal,
            Waktu = waktu,
            Keperluan = request.Keperluan,
            Status = "Menunggu",
            KodeQr = GenerateQrCode()
        };

        _context.JanjiTemus.Add(appointment);
        await _context.SaveChangesAsync();

        return new AppointmentResponse
        {
            IdJanjiTemu = appointment.IdJanjiTemu,
            Tanggal = appointment.Tanggal.ToString("yyyy-MM-dd"),
            Waktu = appointment.Waktu.ToString("HH:mm"),
            Status = appointment.Status,
            Keperluan = appointment.Keperluan,
            KodeQr = appointment.KodeQr,
            Tamu = new TamuResponse
            {
                IdTamu = tamu.IdTamu,
                Nama = tamu.Nama,
                Telepon = tamu.Telepon
            },
            Guru = new UserResponse
            {
                IdPengguna = idGuru,
                Nama = guru.Nama
            }
        };
    }

    public async Task<AppointmentResponse> UpdateAppointmentStatus(int id, string status, WebSocketHandler webSocketHandler)
    {
        var appointment = await _context.JanjiTemus
            .Include(j => j.IdGuruNavigation)
            .Include(j => j.IdTamuNavigation)
            .FirstOrDefaultAsync(j => j.IdJanjiTemu == id);

        if (appointment == null)
        {
            throw new KeyNotFoundException("Janji temu tidak ditemukan");
        }

        // status
        var validStatuses = new[] { "Menunggu", "Selesai", "Telat" };
        if (!validStatuses.Contains(status))
        {
            throw new ArgumentException($"Status harus salah satu dari: {string.Join(", ", validStatuses)}");
        }

        appointment.Status = status;
        await _context.SaveChangesAsync();

        // websocket notif create 
        var message = $"Janji temu dengan {appointment.IdTamuNavigation.Nama} telah sampai di lobby sekolah, segera temui! ";
        await _notificationService.CreateNotification(new NotificationCreateRequest
        {
            IdPengguna = appointment.IdGuru,
            Pesan = message
        });

        // broadcast notif
        await webSocketHandler.BroadcastNotification(appointment.IdGuru, message);

        return new AppointmentResponse
        {
            IdJanjiTemu = appointment.IdJanjiTemu,
            Tanggal = appointment.Tanggal.ToString("yyyy-MM-dd"),
            Waktu = appointment.Waktu.ToString("HH:mm"),
            Status = appointment.Status,
            Keperluan = appointment.Keperluan,
            KodeQr = appointment.KodeQr,
            Tamu = new TamuResponse
            {
                IdTamu = appointment.IdTamuNavigation.IdTamu,
                Nama = appointment.IdTamuNavigation.Nama,
                Telepon = appointment.IdTamuNavigation.Telepon
            },
            Guru = new UserResponse
            {
                IdPengguna = appointment.IdGuruNavigation.IdPengguna,
                Nama = appointment.IdGuruNavigation.Nama
            }
        };
    }

    public async Task<AppointmentResponse> VerifyQrCode(string kode)
    {
        var appointment = await _context.JanjiTemus
            .Include(j => j.IdTamuNavigation)
            .Include(j => j.IdGuruNavigation)
            .FirstOrDefaultAsync(j => j.KodeQr == kode);

        if (appointment == null)
        {
            throw new KeyNotFoundException("Kode QR tidak valid");
        }

        if (appointment.Tanggal != DateOnly.FromDateTime(DateTime.Today))
        {
            throw new InvalidOperationException("Janji temu tidak untuk hari ini");
        }

        if (appointment.Status != "Menunggu")
        {
            throw new InvalidOperationException($"Janji temu sudah dalam status {appointment.Status}");
        }

        return new AppointmentResponse
        {
            IdJanjiTemu = appointment.IdJanjiTemu,
            Tanggal = appointment.Tanggal.ToString("yyyy-MM-dd"),
            Waktu = appointment.Waktu.ToString("HH:mm"),
            Status = appointment.Status,
            Keperluan = appointment.Keperluan,
            KodeQr = appointment.KodeQr,
            Tamu = new TamuResponse
            {
                IdTamu = appointment.IdTamuNavigation.IdTamu,
                Nama = appointment.IdTamuNavigation.Nama,
                Telepon = appointment.IdTamuNavigation.Telepon
            },
            Guru = new UserResponse
            {
                IdPengguna = appointment.IdGuruNavigation.IdPengguna,
                Nama = appointment.IdGuruNavigation.Nama
            }
        };
    }

    public async Task<IEnumerable<AppointmentResponse>> GetTodayAppointments()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        return await _context.JanjiTemus
            .Include(j => j.IdTamuNavigation)
            .Include(j => j.IdGuruNavigation)
            .Where(j => j.Tanggal == today)
            .OrderBy(j => j.Waktu)
            .Select(j => new AppointmentResponse
            {
                IdJanjiTemu = j.IdJanjiTemu,
                Tanggal = j.Tanggal.ToString("yyyy-MM-dd"),
                Waktu = j.Waktu.ToString("HH:mm"),
                Status = j.Status,
                Keperluan = j.Keperluan,
                KodeQr = j.KodeQr,
                Tamu = new TamuResponse
                {
                    IdTamu = j.IdTamuNavigation.IdTamu,
                    Nama = j.IdTamuNavigation.Nama,
                    Telepon = j.IdTamuNavigation.Telepon
                },
                Guru = new UserResponse
                {
                    IdPengguna = j.IdGuruNavigation.IdPengguna,
                    Nama = j.IdGuruNavigation.Nama
                }
            })
            .ToListAsync();
    }

    private string GenerateQrCode()
    {
        return Guid.NewGuid().ToString("N")[..10].ToUpper();
    }
}
