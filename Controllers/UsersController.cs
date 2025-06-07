using BukuTamuAPI.DTOs;
using BukuTamuAPI.Models;
using BukuTamuAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace BukuTamuAPI.Controllers; 

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/users")]    
[ApiController]
[Authorize(Policy = "AdminOnly")]

public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<UserResponse>>> GetAllUsers(
        [FromQuery] string? role,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10)
    {
        try
        {
            var (users, totalCount) = await _userService.GetAllUsers(role, page, limit);
            
            return Ok(new PagedResponse<UserResponse>
            {
                Total = totalCount,
                Page = page,
                Limit = limit,
                Data = users
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat mengambil daftar user");
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponse>> GetUserById(int id)
    {
        try
        {
            var user = await _userService.GetUserById(id);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User dengan ID {Id} tidak ditemukan", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat mengambil user dengan ID {Id}", id);
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser([FromBody] UserCreateRequest request)
    {
        try
        {
            var newUser = await _userService.CreateUser(request);
            return CreatedAtAction(nameof(GetUserById), new { id = newUser.IdPengguna }, newUser);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validasi gagal saat membuat user");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat membuat user baru");
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserResponse>> UpdateUser(int id, [FromBody] UserUpdateRequest request)
    {
        try
        {
            var updatedUser = await _userService.UpdateUser(id, request);
            return Ok(updatedUser);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User dengan ID {Id} tidak ditemukan", id);
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validasi gagal saat mengupdate user");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat mengupdate user dengan ID {Id}", id);
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try
        {
            await _userService.DeleteUser(id);
            return Ok(new { message = "User berhasil dihapus" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User dengan ID {Id} tidak ditemukan", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "User dengan ID {Id} tidak dapat dihapus", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat menghapus user dengan ID {Id}", id);
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }
}