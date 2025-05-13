using System;
using System.Collections.Generic;

namespace BukuTamuAPI.Models;

public partial class RefreshToken
{
    public int Id { get; set; }

    public int IdPengguna { get; set; }

    public string Token { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public bool? Revoked { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Pengguna IdPenggunaNavigation { get; set; } = null!;
}
