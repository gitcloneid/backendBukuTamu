using System;
using System.Collections.Generic;

namespace BukuTamuAPI.Models;

public partial class Notifikasi
{
    public int IdNotifikasi { get; set; }

    public int IdPengguna { get; set; }

    public string Pesan { get; set; } = null!;

    public DateTime? Waktu { get; set; }

    public bool? IsRead { get; set; }

    public virtual Pengguna IdPenggunaNavigation { get; set; } = null!;
}
