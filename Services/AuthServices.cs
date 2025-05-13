namespace BukuTamuAPI.Services; 

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BukuTamuAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BukuTamuAPI.DTOs;

public interface IAuthService
{
    Task<LoginResponse> Login(LoginRequest request);
    Task Logout(string token);
    Task ChangePassword(int userId, ChangePasswordRequest request);
    Task<RefreshTokenResponse> RefreshToken(RefreshTokenRequest request);
}

public class AuthService : IAuthService
{
    private readonly DbtamuContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(DbtamuContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<LoginResponse> Login(LoginRequest request)
    {
        var user = await _context.Penggunas
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        {
            throw new UnauthorizedAccessException("Email atau password salah");
        }

        var token = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken(user.IdPengguna);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken.Token,
            User = new UserResponse
            {
                IdPengguna = user.IdPengguna,
                Nama = user.Nama,
                Email = user.Email,
                Role = user.Role
            }
        };
    }

    public async Task Logout(string token)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);

        if (refreshToken != null)
        {
            _context.RefreshTokens.Remove(refreshToken);
            await _context.SaveChangesAsync();
        }
    }

    public async Task ChangePassword(int userId, ChangePasswordRequest request)
    {
        var user = await _context.Penggunas.FindAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User tidak ditemukan");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
        {
            throw new UnauthorizedAccessException("Password saat ini salah");
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();
    }

    public async Task<RefreshTokenResponse> RefreshToken(RefreshTokenRequest request)
    {
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.IdPenggunaNavigation)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (storedToken == null || storedToken.Revoked.GetValueOrDefault() || storedToken.ExpiresAt < DateTime.UtcNow)
        {
            throw new SecurityTokenException("Refresh token tidak valid atau sudah kadaluarsa");
        }

        var newAccessToken = GenerateJwtToken(storedToken.IdPenggunaNavigation);
        var newRefreshToken = GenerateRefreshToken(storedToken.IdPengguna);

        storedToken.Revoked = true;
        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync();

        return new RefreshTokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresIn = int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"]) * 60
        };
    }

    private string GenerateJwtToken(Pengguna user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.IdPengguna.ToString()),
                new Claim(ClaimTypes.Name, user.Nama),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            }),
            Expires = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"])),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private RefreshToken GenerateRefreshToken(int userId)
    {
        return new RefreshToken
        {
            IdPengguna = userId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"])),
            CreatedAt = DateTime.UtcNow,
            Revoked = false
        };
    }
}
