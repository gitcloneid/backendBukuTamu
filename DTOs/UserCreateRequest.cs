using System.ComponentModel.DataAnnotations;

namespace BukuTamuAPI.DTOs;


public class UserCreateRequest
{
    [Required]
    [StringLength(255)]
    public string Nama { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; }

    [Required]
    [StringLength(50)]
    public string Role { get; set; }
}


public class UserUpdateRequest
{
    [StringLength(255)]
    public string Nama { get; set; }

    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }

    [StringLength(50)]
    public string Role { get; set; }
}


public class PagedResponse<T>
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int Limit { get; set; }
    public IEnumerable<T> Data { get; set; }
}