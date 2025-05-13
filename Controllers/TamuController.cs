using BukuTamuAPI.DTOs;
using BukuTamuAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BukuTamuAPI.Controllers;

[Route("api/tamu")]
[ApiController]
[Authorize]

public class TamuController : ControllerBase
{
    private readonly ITamuService _tamuService;
    private readonly ILogger<TamuController> _logger;

    public TamuController(ITamuService tamuService, ILogger<TamuController> logger)
    {
        _tamuService = tamuService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<TamuResponse>>> GetAllTamu(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10)
    {
        try
        {
            var (tamu, totalCount) = await _tamuService.GetAllTamu(page, limit);
            
            return Ok(new PagedResponse<TamuResponse>
            {
                Total = totalCount,
                Page = page,
                Limit = limit,
                Data = tamu
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat mengambil daftar tamu");
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<TamuResponse>> CreateTamu([FromBody] TamuCreateRequest request)
    {
        try
        {
            var newTamu = await _tamuService.CreateTamu(request);
            return CreatedAtAction(nameof(CreateTamu), new { id = newTamu.IdTamu }, newTamu);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat membuat tamu baru");
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TamuResponse>> UpdateTamu(int id, [FromBody] TamuUpdateRequest request)
    {
        try
        {
            var updatedTamu = await _tamuService.UpdateTamu(id, request);
            return Ok(updatedTamu);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Tamu dengan ID {Id} tidak ditemukan", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat mengupdate tamu dengan ID {Id}", id);
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<TamuResponse>>> SearchTamu([FromQuery] string q)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return BadRequest(new { message = "Query pencarian tidak boleh kosong" });
            }

            var results = await _tamuService.SearchTamu(q);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat mencari tamu dengan query: {Query}", q);
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }
}