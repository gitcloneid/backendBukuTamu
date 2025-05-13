namespace BukuTamuAPI.DTOs;


public class NotificationResponse
{
    public int IdNotifikasi { get; set; }
    public string Pesan { get; set; }
    public DateTime Waktu { get; set; }
    public bool IsRead { get; set; }
}


