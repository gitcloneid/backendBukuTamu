using System.Globalization;
using BukuTamuAPI.DTOs;
using BukuTamuAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BukuTamuAPI.Services;

public interface IReportService
{
    Task<DailyReportResponse> GetDailyReport(DateTime? date = null);
    Task<WeeklyReportResponse> GetWeeklyReport(int? week = null, int? year = null);
    Task<MonthlyReportResponse> GetMonthlyReport(int? month = null, int? year = null);
}

public class ReportService : IReportService
{
    private readonly DbtamuContext _context;
    private readonly ILogger<ReportService> _logger;

    public ReportService(DbtamuContext context, ILogger<ReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DailyReportResponse> GetDailyReport(DateTime? date = null)
    {
        var reportDate = date ?? DateTime.Today;
        var startDate = new DateTime(reportDate.Year, reportDate.Month, reportDate.Day, 0, 0, 0);
        var endDate = new DateTime(reportDate.Year, reportDate.Month, reportDate.Day, 23, 59, 59);

        // Use shadow property TanggalWaktu for date filtering
        var janjiTemuList = await _context.JanjiTemus
            .Include(j => j.IdGuruNavigation)
            .Include(j => j.IdTamuNavigation)
            .Where(j => EF.Property<DateTime>(j, "TanggalWaktu") >= startDate &&
                       EF.Property<DateTime>(j, "TanggalWaktu") <= endDate)
            .ToListAsync();

        // Populate the ignored properties from shadow property
        foreach (var janjiTemu in janjiTemuList)
        {
            janjiTemu.LoadFromTanggalWaktu(_context);
        }

        var appointments = janjiTemuList;

        var total = appointments.Count;
        var completed = appointments.Count(j => j.Status == "Selesai");
        var waiting = appointments.Count(j => j.Status == "Menunggu");
        var late = appointments.Count(j => j.Status == "Telat");

        var byTeacher = appointments
            .GroupBy(j => new { j.IdGuru, j.IdGuruNavigation.Nama })
            .Select(g => new TeacherStat
            {
                IdGuru = g.Key.IdGuru,
                Nama = g.Key.Nama,
                Total = g.Count(),
                Completed = g.Count(j => j.Status == "Selesai")
            })
            .ToList();

        return new DailyReportResponse
        {
            Date = reportDate.ToString("yyyy-MM-dd"),
            TotalAppointments = total,
            Completed = completed,
            Waiting = waiting,
            Late = late,
            CompletionRate = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0,
            ByTeacher = byTeacher
        };
    }

    public async Task<WeeklyReportResponse> GetWeeklyReport(int? week = null, int? year = null)
    {
        var currentDate = DateTime.Today;
        var targetYear = year ?? currentDate.Year;
        var targetWeek = week ?? ISOWeek.GetWeekOfYear(currentDate);

        // week dalam 1 tahun bukan 1 bulan
        if (targetWeek < 1 || targetWeek > 53)
        {
            throw new ArgumentException("Week number must be between 1 and 53");
        }
        if (targetYear < 1900 || targetYear > 9999)
        {
            throw new ArgumentException("Year must be between 1900 and 9999");
        }

        var startDate = ISOWeek.ToDateTime(targetYear, targetWeek, DayOfWeek.Monday);
        var endDate = startDate.AddDays(6);
        var startDateTime = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0);
        var endDateTime = new DateTime(endDate.Year, endDate.Month, endDate.Day, 23, 59, 59);

        // Use shadow property TanggalWaktu for date filtering
        var janjiTemuList = await _context.JanjiTemus
            .Include(j => j.IdGuruNavigation)
            .Include(j => j.IdTamuNavigation)
            .Where(j => EF.Property<DateTime>(j, "TanggalWaktu") >= startDateTime &&
                       EF.Property<DateTime>(j, "TanggalWaktu") <= endDateTime)
            .ToListAsync();

        // Populate the ignored properties from shadow property
        foreach (var janjiTemu in janjiTemuList)
        {
            janjiTemu.LoadFromTanggalWaktu(_context);
        }

        var appointments = janjiTemuList;

        var dailyStats = new List<DailyStat>();
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var dateOnly = DateOnly.FromDateTime(date);
            var dateAppointments = appointments
                .Where(j => j.Tanggal == dateOnly)
                .ToList();

            dailyStats.Add(new DailyStat
            {
                Date = date.ToString("yyyy-MM-dd"),
                Total = dateAppointments.Count,
                Completed = dateAppointments.Count(j => j.Status == "Selesai")
            });
        }

        var total = appointments.Count;
        var completed = appointments.Count(j => j.Status == "Selesai");

        return new WeeklyReportResponse
        {
            WeekNumber = targetWeek,
            Year = targetYear,
            TotalAppointments = total,
            CompletionRate = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0,
            DailyStats = dailyStats
        };
    }

    public async Task<MonthlyReportResponse> GetMonthlyReport(int? month = null, int? year = null)
    {
        var currentDate = DateTime.Today;
        var targetMonth = month ?? currentDate.Month;
        var targetYear = year ?? currentDate.Year;

        // Validasi bulan
        if (targetMonth < 1 || targetMonth > 12)
        {
            throw new ArgumentException("Month must be between 1 and 12");
        }
        if (targetYear < 1900 || targetYear > 9999)
        {
            throw new ArgumentException("Year must be between 1900 and 9999");
        }

        var startDate = new DateTime(targetYear, targetMonth, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var startDateTime = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0);
        var endDateTime = new DateTime(endDate.Year, endDate.Month, endDate.Day, 23, 59, 59);

        // Use shadow property TanggalWaktu for date filtering
        var janjiTemuList = await _context.JanjiTemus
            .Include(j => j.IdGuruNavigation)
            .Include(j => j.IdTamuNavigation)
            .Where(j => EF.Property<DateTime>(j, "TanggalWaktu") >= startDateTime &&
                       EF.Property<DateTime>(j, "TanggalWaktu") <= endDateTime)
            .ToListAsync();

        // Populate the ignored properties from shadow property
        foreach (var janjiTemu in janjiTemuList)
        {
            janjiTemu.LoadFromTanggalWaktu(_context);
        }

        var appointments = janjiTemuList;

        var weeklyStats = new List<WeeklyStat>();
        var currentWeekStart = startDate;
        int weekNumberInMonth = 1; // Counter untuk minggu ke-berapa dalam bulan

        while (currentWeekStart <= endDate)
        {
            var currentWeekEnd = currentWeekStart.AddDays(6) > endDate ?
                endDate : currentWeekStart.AddDays(6);

            var currentWeekStartDateOnly = DateOnly.FromDateTime(currentWeekStart);
            var currentWeekEndDateOnly = DateOnly.FromDateTime(currentWeekEnd);

            var weekAppointments = appointments
                .Where(j => j.Tanggal >= currentWeekStartDateOnly &&
                            j.Tanggal <= currentWeekEndDateOnly)
                .ToList();

            weeklyStats.Add(new WeeklyStat
            {
                WeekNumber = weekNumberInMonth,
                Total = weekAppointments.Count,
                Completed = weekAppointments.Count(j => j.Status == "Selesai")
            });

            currentWeekStart = currentWeekStart.AddDays(7);
            weekNumberInMonth++; // Increment minggu dalam bulan
        }

        var total = appointments.Count;
        var completed = appointments.Count(j => j.Status == "Selesai");

        return new MonthlyReportResponse
        {
            Month = targetMonth,
            Year = targetYear,
            TotalAppointments = total,
            CompletionRate = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0,
            WeeklyStats = weeklyStats
        };
    }
}