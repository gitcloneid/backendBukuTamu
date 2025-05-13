using System.ComponentModel.DataAnnotations;

namespace BukuTamuAPI.DTOs;


public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }
}


public class LoginResponse
{
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public UserResponse User { get; set; }
}


public class UserResponse
{
    public int IdPengguna { get; set; }
    public string Nama { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
}


public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; }

    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; }
}


public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; }
}


public class RefreshTokenResponse
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
}