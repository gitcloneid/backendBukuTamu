using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

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

    // Helper method to set the shadow property when saving
    public void SetTanggalWaktu(DbContext context)
    {
        var dateTime = new DateTime(Tanggal.Year, Tanggal.Month, Tanggal.Day,
            Waktu.Hour, Waktu.Minute, Waktu.Second);
        context.Entry(this).Property("TanggalWaktu").CurrentValue = dateTime;
    }

    // Helper method to get values from the shadow property when loading
    public void LoadFromTanggalWaktu(DbContext context)
    {
        var dateTime = (DateTime)context.Entry(this).Property("TanggalWaktu").CurrentValue;
        Tanggal = DateOnly.FromDateTime(dateTime);
        Waktu = TimeOnly.FromDateTime(dateTime);
    }

    // Alternative helper method to get the combined DateTime value
    public DateTime GetTanggalWaktu(DbContext context)
    {
        return (DateTime)context.Entry(this).Property("TanggalWaktu").CurrentValue;
    }
}