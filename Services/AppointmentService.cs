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
    Task<AppointmentResponse> CheckAppointmentByQrCode(string kode);
    Task<IEnumerable<AppointmentResponse>> GetTodayAppointments();
    Task<AppointmentResponse> RescheduleAppointment(int id, AppointmentRescheduleRequest request, WebSocketHandler webSocketHandler);
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
            // Convert DateOnly to DateTime range for comparison with shadow property
            var startDate = new DateTime(date.Value.Year, date.Value.Month, date.Value.Day, 0, 0, 0);
            var endDate = new DateTime(date.Value.Year, date.Value.Month, date.Value.Day, 23, 59, 59);
            query = query.Where(j => EF.Property<DateTime>(j, "TanggalWaktu") >= startDate &&
                                    EF.Property<DateTime>(j, "TanggalWaktu") <= endDate);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(j => j.Status == status);
        }

        var totalCount = await query.CountAsync();

        // Load data and then populate Tanggal/Waktu from shadow property
        var janjiTemuList = await query
            .OrderBy(j => EF.Property<DateTime>(j, "TanggalWaktu"))
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        // Populate the ignored properties from shadow property
        foreach (var janjiTemu in janjiTemuList)
        {
            janjiTemu.LoadFromTanggalWaktu(_context);
        }

        var appointments = janjiTemuList.Select(j => new AppointmentResponse
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
        }).ToList();

        return (appointments, totalCount);
    }

    public async Task<IEnumerable<AppointmentResponse>> GetAppointmentsByGuru(int idGuru, DateOnly? date, string? status)
    {
        var query = _context.JanjiTemus
            .Include(j => j.IdTamuNavigation)
            .Where(j => j.IdGuru == idGuru);

        if (date.HasValue)
        {
            // Convert DateOnly to DateTime range for comparison with shadow property
            var startDate = new DateTime(date.Value.Year, date.Value.Month, date.Value.Day, 0, 0, 0);
            var endDate = new DateTime(date.Value.Year, date.Value.Month, date.Value.Day, 23, 59, 59);
            query = query.Where(j => EF.Property<DateTime>(j, "TanggalWaktu") >= startDate &&
                                    EF.Property<DateTime>(j, "TanggalWaktu") <= endDate);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(j => j.Status == status);
        }

        var janjiTemuList = await query
            .OrderBy(j => EF.Property<DateTime>(j, "TanggalWaktu"))
            .ToListAsync();

        // Populate the ignored properties from shadow property
        foreach (var janjiTemu in janjiTemuList)
        {
            janjiTemu.LoadFromTanggalWaktu(_context);
        }

        return janjiTemuList.Select(j => new AppointmentResponse
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
        }).ToList();
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

        // Set the shadow property before saving
        appointment.SetTanggalWaktu(_context);

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

        // Populate ignored properties from shadow property
        appointment.LoadFromTanggalWaktu(_context);

        // Validate status
        var validStatuses = new[] { "Menunggu", "Selesai", "Telat" };
        if (!validStatuses.Contains(status))
        {
            throw new ArgumentException($"Status harus salah satu dari: {string.Join(", ", validStatuses)}");
        }

        // Check if status is changing from Menunggu to Telat or Selesai
        bool sendNotification = appointment.Status == "Menunggu" && (status == "Telat" || status == "Selesai");

        appointment.Status = status;
        await _context.SaveChangesAsync();

        // Send notification only for transitions from Menunggu to Telat or Selesai
        if (sendNotification)
        {
            var culture = new System.Globalization.CultureInfo("id-ID");

            string message = status switch
            {
                "Telat" => $"Kami informasikan bahwa Bapak/Ibu {appointment.IdTamuNavigation.Nama} telah tiba di lobby sekolah untuk janji temu, namun terlambat dari jadwal ({appointment.Tanggal.ToString("dd MMMM yyyy", culture)} pukul {appointment.Waktu:HH:mm}). Silakan temui beliau.",
                "Selesai" => $"Kami informasikan bahwa Bapak/Ibu {appointment.IdTamuNavigation.Nama} sudah berada di lobby sekolah sesuai jadwal janji temu. Silakan temui beliau.",
                _ => throw new InvalidOperationException("Unexpected status for notification")
            };


            // Create database notification
            await _notificationService.CreateNotification(new NotificationCreateRequest
            {
                IdPengguna = appointment.IdGuru,
                Pesan = message
            });

            // Broadcast notification via WebSocket
            await webSocketHandler.BroadcastNotification(appointment.IdGuru, message);
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

        // Populate ignored properties from shadow property
        appointment.LoadFromTanggalWaktu(_context);

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

    public async Task<AppointmentResponse> CheckAppointmentByQrCode(string kode)
    {
        var appointment = await _context.JanjiTemus
            .Include(j => j.IdTamuNavigation)
            .Include(j => j.IdGuruNavigation)
            .FirstOrDefaultAsync(j => j.KodeQr == kode);

        if (appointment == null)
        {
            throw new KeyNotFoundException("Kode QR tidak valid");
        }

        // Populate ignored properties from shadow property
        appointment.LoadFromTanggalWaktu(_context);

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
        var startDate = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0);
        var endDate = new DateTime(today.Year, today.Month, today.Day, 23, 59, 59);

        var janjiTemuList = await _context.JanjiTemus
            .Include(j => j.IdTamuNavigation)
            .Include(j => j.IdGuruNavigation)
            .Where(j => EF.Property<DateTime>(j, "TanggalWaktu") >= startDate &&
                       EF.Property<DateTime>(j, "TanggalWaktu") <= endDate)
            .OrderBy(j => EF.Property<DateTime>(j, "TanggalWaktu"))
            .ToListAsync();

        // Populate the ignored properties from shadow property
        foreach (var janjiTemu in janjiTemuList)
        {
            janjiTemu.LoadFromTanggalWaktu(_context);
        }

        return janjiTemuList.Select(j => new AppointmentResponse
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
        }).ToList();
    }

    public async Task<AppointmentResponse> RescheduleAppointment(int id, AppointmentRescheduleRequest request, WebSocketHandler webSocketHandler)
    {
        if (!DateOnly.TryParseExact(request.Tanggal, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var tanggal))
        {
            throw new ArgumentException("Tanggal must be in yyyy-MM-dd format");
        }

        if (!TimeOnly.TryParseExact(request.Waktu, "HH:mm", null, System.Globalization.DateTimeStyles.None, out var waktu))
        {
            throw new ArgumentException("Waktu must be in HH:mm format");
        }

        var appointment = await _context.JanjiTemus
            .Include(j => j.IdTamuNavigation)
            .Include(j => j.IdGuruNavigation)
            .FirstOrDefaultAsync(j => j.IdJanjiTemu == id);

        if (appointment == null)
        {
            throw new KeyNotFoundException("Janji temu tidak ditemukan");
        }

        var currentRescheduleStatus = _context.Entry(appointment).Property<string>("reschedule").CurrentValue;
        if (currentRescheduleStatus == RescheduleStatus.Batal)
        {
            var batalMessage = $"Pengajuan reschedule untuk janji temu dengan {appointment.IdTamuNavigation.Nama} (ID: {appointment.IdJanjiTemu}) karena sebelumnya dibatalkan.";
            await webSocketHandler.BroadcastNotification(appointment.IdGuru, batalMessage);

            await _notificationService.CreateNotification(new NotificationCreateRequest
            {
                IdPengguna = appointment.IdGuru,
                Pesan = batalMessage
            });
        }

        appointment.Tanggal = tanggal;
        appointment.Waktu = waktu;

        appointment.SetTanggalWaktu(_context);


        if (!RescheduleStatus.IsValid(RescheduleStatus.Tunggu))
        {
            throw new ArgumentException("Invalid reschedule status");
        }
        _context.Entry(appointment).Property("reschedule").CurrentValue = RescheduleStatus.Tunggu;

        await _context.SaveChangesAsync();

        var culture = new System.Globalization.CultureInfo("id-ID");

        var rescheduleMessage = $"Janji temu dengan Bapak/Ibu {appointment.IdTamuNavigation.Nama} pada {appointment.Tanggal.ToString("dd MMMM yyyy", culture)} pukul {appointment.Waktu:HH:mm} telah dijadwalkan ulang.";
        await _notificationService.CreateNotification(new NotificationCreateRequest
        {
            IdPengguna = appointment.IdGuru,
            Pesan = rescheduleMessage
        });

        await webSocketHandler.BroadcastNotification(appointment.IdGuru, rescheduleMessage);

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

    private string GenerateQrCode()
    {
        return Guid.NewGuid().ToString("N")[..10].ToUpper();
    }
}