using BukuTamuAPI.DTOs;
using BukuTamuAPI.Services;

namespace BukuTamuAPI.Controllers; 

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.Login(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Login gagal untuk email: {Email}", request.Email);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat login untuk email: {Email}", request.Email);
            return StatusCode(500, new { message = "Terjadi kesalahan internal" });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            await _authService.Logout(token);
            return Ok(new { message = "Logout berhasil" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat logout");
            return StatusCode(500, new { message = "Terjadi kesalahan internal saat logout" });
        }
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            await _authService.ChangePassword(userId, request);
            return Ok(new { message = "Password berhasil diubah" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "User tidak ditemukan");
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Password saat ini salah");
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validasi password gagal");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat mengubah password");
            return StatusCode(500, new { message = "Terjadi kesalahan internal saat mengubah password" });
        }
    }
    
    [HttpPost("refresh-token")]
    public async Task<ActionResult<RefreshTokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var response = await _authService.RefreshToken(request);
            return Ok(response);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Refresh token tidak valid");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saat refresh token");
            return StatusCode(500, new { message = "Terjadi kesalahan internal saat refresh token" });
        }
    }
}