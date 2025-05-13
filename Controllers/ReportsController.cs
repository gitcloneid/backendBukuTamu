using BukuTamuAPI.DTOs;
using BukuTamuAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BukuTamuAPI.Controllers;

[Route("api/reports")]
[ApiController]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    [HttpGet("daily")]
    public async Task<ActionResult<DailyReportResponse>> GetDailyReport([FromQuery] DateTime? date)
    {
        try
        {
            var report = await _reportService.GetDailyReport(date);
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating daily report for date {Date}", date?.ToString("yyyy-MM-dd") ?? "today");
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }

    [HttpGet("weekly")]
    public async Task<ActionResult<WeeklyReportResponse>> GetWeeklyReport(
        [FromQuery] int? week, 
        [FromQuery] int? year)
    {
        try
        {
            var report = await _reportService.GetWeeklyReport(week, year);
            return Ok(report);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters for weekly report: week={Week}, year={Year}", week, year);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating weekly report for week {Week}, year {Year}", week, year);
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }

    [HttpGet("monthly")]
    public async Task<ActionResult<MonthlyReportResponse>> GetMonthlyReport(
        [FromQuery] int? month, 
        [FromQuery] int? year)
    {
        try
        {
            var report = await _reportService.GetMonthlyReport(month, year);
            return Ok(report);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters for monthly report: month={Month}, year={Year}", month, year);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating monthly report for month {Month}, year {Year}", month, year);
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }
}