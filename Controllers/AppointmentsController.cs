using System.Security.Claims;
using BukuTamuAPI.DTOs;
using BukuTamuAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BukuTamuAPI.Controllers;

[Route("api/appointments")]
[ApiController]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ILogger<AppointmentsController> _logger;
    private readonly WebSocketHandler _webSocketHandler;

    public AppointmentsController(
        IAppointmentService appointmentService,
        ILogger<AppointmentsController> logger,
        WebSocketHandler webSocketHandler)
    {
        _appointmentService = appointmentService;
        _logger = logger;
        _webSocketHandler = webSocketHandler;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<AppointmentResponse>>> GetAllAppointments(
        [FromQuery] DateOnly? date,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10)
    {
        try
        {
            var (appointments, totalCount) = await _appointmentService.GetAllAppointments(date, status, page, limit);
            return Ok(new PagedResponse<AppointmentResponse>
            {
                Total = totalCount,
                Page = page,
                Limit = limit,
                Data = appointments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat mengambil daftar janji temu");
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }

    [HttpGet("guru/{id}")]
    public async Task<ActionResult<IEnumerable<AppointmentResponse>>> GetAppointmentsByGuru(
        int id,
        [FromQuery] DateOnly? date,
        [FromQuery] string? status)
    {
        try
        {
            var appointments = await _appointmentService.GetAppointmentsByGuru(id, date, status);
            return Ok(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat mengambil janji temu guru");
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<AppointmentResponse>> CreateAppointment([FromBody] AppointmentCreateRequest request)
    {
        try
        {
            // Get teacher ID from JWT claims
            var teacherIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(teacherIdClaim) || !int.TryParse(teacherIdClaim, out var teacherId))
            {
                _logger.LogWarning("Invalid or missing NameIdentifier claim");
                return Unauthorized(new { message = "Invalid user authentication" });
            }

            // Verify the user is actually a teacher
            if (!User.IsInRole("Guru"))
            {
                _logger.LogWarning("User with ID {TeacherId} is not a Guru", teacherId);
                return Forbid();
            }

            var newAppointment = await _appointmentService.CreateAppointment(request, teacherId);
            return CreatedAtAction(nameof(GetAllAppointments), new { id = newAppointment.IdJanjiTemu }, newAppointment);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid input for creating appointment");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat membuat janji temu baru");
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<AppointmentResponse>> UpdateAppointmentStatus(
        int id,
        [FromBody] string status)
    {
        try
        {
            var updatedAppointment = await _appointmentService.UpdateAppointmentStatus(id, status, _webSocketHandler);
            return Ok(updatedAppointment);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Janji temu dengan ID {Id} tidak ditemukan", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat mengupdate status janji temu dengan ID {Id}", id);
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }

    [HttpPut("{id}/reschedule")]
    public async Task<ActionResult<AppointmentResponse>> RescheduleAppointment(
        int id,
        [FromBody] AppointmentRescheduleRequest request)
    {
        try
        {
            var updatedAppointment = await _appointmentService.RescheduleAppointment(id, request, _webSocketHandler);
            return Ok(updatedAppointment);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Janji temu dengan ID {Id} tidak ditemukan", id);
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid input for rescheduling appointment with ID {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat menjadwalkan ulang janji temu dengan ID {Id}", id);
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }

    [HttpGet("qr/{kode}")]
    public async Task<ActionResult<AppointmentResponse>> CheckAppointmentByQrCode(string kode)
    {
        try
        {
            var appointment = await _appointmentService.CheckAppointmentByQrCode(kode);
            return Ok(appointment);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Kode qr tidak valid", kode);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "error validasi", kode);
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }

    [HttpGet("today")]
    public async Task<ActionResult<IEnumerable<AppointmentResponse>>> GetTodayAppointments()
    {
        try
        {
            var appointments = await _appointmentService.GetTodayAppointments();
            return Ok(appointments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat mengambil janji temu hari ini");
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }
}