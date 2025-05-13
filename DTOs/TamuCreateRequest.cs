using System.ComponentModel.DataAnnotations;

namespace BukuTamuAPI.DTOs;


public class TamuResponse
{
    public int IdTamu { get; set; }
    public string Nama { get; set; }
    public string Telepon { get; set; }
}


public class TamuCreateRequest
{
    [Required]
    [StringLength(255)]
    public string Nama { get; set; }

    [Required]
    [StringLength(20)]
    public string Telepon { get; set; }
}


public class TamuUpdateRequest
{
    [StringLength(255)]
    public string Nama { get; set; }

    [StringLength(20)]
    public string Telepon { get; set; }
}

