using Microsoft.EntityFrameworkCore;

namespace BukuTamuAPI.Models;

public partial class DbtamuContext : DbContext
{
    public DbtamuContext() { }

    public DbtamuContext(DbContextOptions<DbtamuContext> options)
        : base(options) { }

    public virtual DbSet<JanjiTemu> JanjiTemus { get; set; }
    public virtual DbSet<Notifikasi> Notifikasis { get; set; }
    public virtual DbSet<Pengguna> Penggunas { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public virtual DbSet<Tamu> Tamus { get; set; }
    public virtual DbSet<Ulasan> Ulasans { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
       //kontol bengkok
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JanjiTemu>(entity =>
        {
            entity.ToTable("janji_temu");
            entity.HasKey(e => e.IdJanjiTemu).HasName("PRIMARY");
            entity.Property(e => e.IdJanjiTemu).HasColumnName("id_janji_temu").ValueGeneratedOnAdd();
            entity.Property(e => e.IdTamu).HasColumnName("id_tamu");
            entity.Property(e => e.IdGuru).HasColumnName("id_guru");
            entity.Property(e => e.Tanggal).HasColumnName("tanggal");
            entity.Property(e => e.Waktu).HasColumnName("waktu");
            entity.Property(e => e.Keperluan).HasMaxLength(255).HasColumnName("keperluan");
            entity.Property(e => e.Status).HasMaxLength(20).HasColumnName("status");
            entity.Property(e => e.KodeQr).HasMaxLength(50).HasColumnName("kode_qr");

            entity.HasOne(d => d.IdGuruNavigation)
                .WithMany(p => p.JanjiTemus)
                .HasForeignKey(d => d.IdGuru)
                .HasConstraintName("FK_janji_temu_pengguna");

            entity.HasOne(d => d.IdTamuNavigation)
                .WithMany(p => p.JanjiTemus)
                .HasForeignKey(d => d.IdTamu)
                .HasConstraintName("FK_janji_temu_tamu");
        });

        modelBuilder.Entity<Notifikasi>(entity =>
        {
            entity.ToTable("notifikasi");
            entity.HasKey(e => e.IdNotifikasi).HasName("PRIMARY");
            entity.Property(e => e.IdNotifikasi).HasColumnName("id_notifikasi").ValueGeneratedOnAdd();
            entity.Property(e => e.IdPengguna).HasColumnName("id_pengguna");
            entity.Property(e => e.Pesan).HasMaxLength(255).HasColumnName("pesan");
            entity.Property(e => e.Waktu).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("waktu");
            entity.Property(e => e.IsRead).HasDefaultValue(false).HasColumnName("is_read");

            entity.HasOne(d => d.IdPenggunaNavigation)
                .WithMany(p => p.Notifikasis)
                .HasForeignKey(d => d.IdPengguna)
                .HasConstraintName("FK_notifikasi_pengguna");
        });

        modelBuilder.Entity<Pengguna>(entity =>
        {
            entity.ToTable("pengguna");
            entity.HasKey(e => e.IdPengguna).HasName("PRIMARY");
            entity.Property(e => e.IdPengguna).HasColumnName("id_pengguna").ValueGeneratedOnAdd();
            entity.Property(e => e.Nama).HasMaxLength(255).HasColumnName("nama");
            entity.Property(e => e.Email).HasMaxLength(255).HasColumnName("email");
            entity.Property(e => e.Password).HasMaxLength(255).HasColumnName("password");
            entity.Property(e => e.Role).HasMaxLength(50).HasColumnName("role");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.IdPengguna).HasColumnName("id_pengguna");
            entity.Property(e => e.Token).HasMaxLength(255).HasColumnName("token");
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime").HasColumnName("expires_at");
            entity.Property(e => e.Revoked).HasDefaultValue(false).HasColumnName("revoked");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("created_at");

            entity.HasOne(d => d.IdPenggunaNavigation)
                .WithMany(p => p.RefreshTokens)
                .HasForeignKey(d => d.IdPengguna)
                .HasConstraintName("FK_refresh_token_pengguna");
        });

        modelBuilder.Entity<Tamu>(entity =>
        {
            entity.ToTable("tamu");
            entity.HasKey(e => e.IdTamu).HasName("PRIMARY");
            entity.Property(e => e.IdTamu).HasColumnName("id_tamu").ValueGeneratedOnAdd();
            entity.Property(e => e.Nama).HasMaxLength(255).HasColumnName("nama");
            entity.Property(e => e.Telepon).HasMaxLength(20).HasColumnName("telepon");
        });

        modelBuilder.Entity<Ulasan>(entity =>
        {
            entity.ToTable("ulasan");
            entity.HasKey(e => e.IdUlasan).HasName("PRIMARY");
            entity.Property(e => e.IdUlasan).HasColumnName("id_ulasan").ValueGeneratedOnAdd();
            entity.Property(e => e.IdJanjiTemu).HasColumnName("id_janji_temu");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.Komentar).HasColumnName("komentar");
            entity.Property(e => e.WaktuUlasan).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP").HasColumnName("waktu_ulasan");

            entity.HasOne(d => d.IdJanjiTemuNavigation)
                .WithMany(p => p.Ulasans)
                .HasForeignKey(d => d.IdJanjiTemu)
                .HasConstraintName("FK_ulasan_janji_temu");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}