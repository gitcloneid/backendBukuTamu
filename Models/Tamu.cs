using System;
using System.Collections.Generic;

namespace BukuTamuAPI.Models;

public partial class Tamu
{
    public int IdTamu { get; set; }

    public string Nama { get; set; } = null!;

    public string Telepon { get; set; } = null!;

    public virtual ICollection<JanjiTemu> JanjiTemus { get; set; } = new List<JanjiTemu>();
}
