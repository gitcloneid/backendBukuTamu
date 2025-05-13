using System;
using System.Collections.Generic;

namespace BukuTamuAPI.Models;

public partial class Pengguna
{
    public int IdPengguna { get; set; }

    public string Nama { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Role { get; set; } = null!;

    public virtual ICollection<JanjiTemu> JanjiTemus { get; set; } = new List<JanjiTemu>();

    public virtual ICollection<Notifikasi> Notifikasis { get; set; } = new List<Notifikasi>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
