using System;
using System.Collections.Generic;

namespace BukuTamuAPI.Models;

public partial class Ulasan
{
    public int IdUlasan { get; set; }

    public int IdJanjiTemu { get; set; }

    public int Rating { get; set; }

    public string? Komentar { get; set; }

    public DateTime? WaktuUlasan { get; set; }

    public virtual JanjiTemu IdJanjiTemuNavigation { get; set; } = null!;
}
