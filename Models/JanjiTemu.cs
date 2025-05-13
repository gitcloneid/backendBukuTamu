using System;
using System.Collections.Generic;

namespace BukuTamuAPI.Models;

public partial class JanjiTemu
{
    public int IdJanjiTemu { get; set; }

    public int IdTamu { get; set; }

    public int IdGuru { get; set; }

    public DateOnly Tanggal { get; set; }

    public TimeOnly Waktu { get; set; }

    public string Keperluan { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string KodeQr { get; set; } = null!;

    public virtual Pengguna IdGuruNavigation { get; set; } = null!;

    public virtual Tamu IdTamuNavigation { get; set; } = null!;

    public virtual ICollection<Ulasan> Ulasans { get; set; } = new List<Ulasan>();
}
