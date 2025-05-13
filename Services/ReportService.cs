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
        var dateOnly = DateOnly.FromDateTime(reportDate);

        var appointments = await _context.JanjiTemus
            .Include(j => j.IdGuruNavigation)
            .Include(j => j.IdTamuNavigation)
            .Where(j => j.Tanggal == dateOnly)
            .ToListAsync();

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

        var appointments = await _context.JanjiTemus
            .Include(j => j.IdGuruNavigation)
            .Include(j => j.IdTamuNavigation)
            .Where(j => j.Tanggal >= DateOnly.FromDateTime(startDate) && 
                        j.Tanggal <= DateOnly.FromDateTime(endDate))
            .ToListAsync();

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

        // bulan
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

        var appointments = await _context.JanjiTemus
            .Include(j => j.IdGuruNavigation)
            .Include(j => j.IdTamuNavigation)
            .Where(j => j.Tanggal >= DateOnly.FromDateTime(startDate) && 
                        j.Tanggal <= DateOnly.FromDateTime(endDate))
            .ToListAsync();

        var weeklyStats = new List<WeeklyStat>();
        var currentWeekStart = startDate;
        while (currentWeekStart <= endDate)
        {
            var currentWeekEnd = currentWeekStart.AddDays(6) > endDate ? 
                endDate : currentWeekStart.AddDays(6);
            var weekNumber = ISOWeek.GetWeekOfYear(currentWeekStart);

            var weekAppointments = appointments
                .Where(j => j.Tanggal >= DateOnly.FromDateTime(currentWeekStart) && 
                            j.Tanggal <= DateOnly.FromDateTime(currentWeekEnd))
                .ToList();

            weeklyStats.Add(new WeeklyStat
            {
                WeekNumber = weekNumber,
                Total = weekAppointments.Count,
                Completed = weekAppointments.Count(j => j.Status == "Selesai")
            });

            currentWeekStart = currentWeekStart.AddDays(7);
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