using System.ComponentModel.DataAnnotations;

namespace BukuTamuAPI.DTOs;


public class AppointmentResponse
{
    public int IdJanjiTemu { get; set; }
    public string Tanggal { get; set; } = string.Empty;
    public string Waktu { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Keperluan { get; set; } = string.Empty;
    public string? KodeQr { get; set; }
    public TamuResponse? Tamu { get; set; }
    public UserResponse? Guru { get; set; }
}


public class AppointmentCreateRequest
{
    [Required(ErrorMessage = "IdTamu is required")]
    public int IdTamu { get; set; }

    [Required(ErrorMessage = "Tanggal is required")]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Tanggal must be in yyyy-MM-dd format")]
    public string Tanggal { get; set; } = string.Empty;

    [Required(ErrorMessage = "Waktu is required")]
    [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Waktu must be in HH:mm format")]
    public string Waktu { get; set; } = string.Empty;

    [Required(ErrorMessage = "Keperluan is required")]
    [StringLength(255, ErrorMessage = "Keperluan cannot exceed 255 characters")]
    public string Keperluan { get; set; } = string.Empty;
}

public class NotificationCreateRequest
{
    public int IdPengguna { get; set; }
    public string Pesan { get; set; }
}