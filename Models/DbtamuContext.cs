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
        // Connection string configured via dependency injection
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JanjiTemu>(entity =>
        {
            entity.ToTable("jadwal_temu");
            entity.HasKey(e => e.IdJanjiTemu).HasName("PRIMARY");
            entity.Property(e => e.IdJanjiTemu).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.IdTamu).HasColumnName("tamu_id");
            entity.Property(e => e.IdGuru).HasColumnName("user_id");
            entity.Property(e => e.Keperluan).HasMaxLength(255).HasColumnName("keterangan");
            entity.Property(e => e.Status).HasColumnType("longtext").HasColumnName("status");
            entity.Property(e => e.KodeQr).HasMaxLength(50).HasColumnName("kode_qr");

            // Ignore the Tanggal property since we'll use the shadow property instead
            entity.Ignore(e => e.Tanggal);

            // Ignore Waktu since it's derived and updated via SetTanggalWaktu
            entity.Ignore(e => e.Waktu);

            // Use only the shadow property for the full datetime mapping
            entity.Property<DateTime>("TanggalWaktu").HasColumnType("datetime(6)").HasColumnName("tanggal");

            // Map relationships
            entity.HasOne(d => d.IdGuruNavigation)
                .WithMany(p => p.JanjiTemus)
                .HasForeignKey(d => d.IdGuru)
                .HasConstraintName("jadwal_temu_user_id_foreign")
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.IdTamuNavigation)
                .WithMany(p => p.JanjiTemus)
                .HasForeignKey(d => d.IdTamu)
                .HasConstraintName("jadwal_temu_tamu_id_foreign")
                .OnDelete(DeleteBehavior.Cascade);

            // Map shadow properties
            entity.Property<string>("reschedule").HasColumnType("longtext").IsRequired(false);
            entity.Property<DateTime?>("created_at").HasColumnType("datetime(6)").IsRequired(false);
            entity.Property<DateTime?>("updated_at").HasColumnType("datetime(6)").IsRequired(false);
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
                .HasConstraintName("notifikasi_id_pengguna_foreign")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Pengguna>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.IdPengguna).HasName("PRIMARY");
            entity.Property(e => e.IdPengguna).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.Nama).HasMaxLength(255).HasColumnName("name");
            entity.Property(e => e.Email).HasMaxLength(255).HasColumnName("email");
            entity.Property(e => e.Password).HasMaxLength(255).HasColumnName("password");
            entity.Property(e => e.Role).HasColumnType("longtext").HasColumnName("role");
            entity.Property<string>("username").HasMaxLength(255).IsRequired(false);
            entity.Property<DateTime?>("email_verified_at").HasColumnType("datetime(6)").IsRequired(false);
            entity.Property<string>("remember_token").HasMaxLength(100).IsRequired(false);
            entity.Property<DateTime?>("created_at").HasColumnType("datetime(6)").IsRequired(false);
            entity.Property<DateTime?>("updated_at").HasColumnType("datetime(6)").IsRequired(false);
            entity.HasIndex(e => e.Email, "users_email_unique").IsUnique();
            //entity.HasIndex(e => e.Username, "users_username_unique").IsUnique();
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
                .HasConstraintName("refresh_tokens_id_pengguna_foreign")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Tamu>(entity =>
        {
            entity.ToTable("tamu");
            entity.HasKey(e => e.IdTamu).HasName("PRIMARY");
            entity.Property(e => e.IdTamu).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.Nama).HasMaxLength(255).HasColumnName("nama");
            entity.Property(e => e.Telepon).HasMaxLength(255).HasColumnName("telepon");
            entity.Property<string>("password").HasMaxLength(255).IsRequired(false);
            entity.Property<DateTime?>("created_at").HasColumnType("datetime(6)").IsRequired(false);
            entity.Property<DateTime?>("updated_at").HasColumnType("datetime(6)").IsRequired(false);
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
                .HasConstraintName("FK_ulasan_janji_temu")
                .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}