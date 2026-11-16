using Microsoft.EntityFrameworkCore;
using PillarSys.Infrastructure.Entities;

namespace PillarSys.Infrastructure.Data;

public sealed class PillarSysDbContext : DbContext
{
    public PillarSysDbContext(DbContextOptions<PillarSysDbContext> options) : base(options) { }

    public DbSet<Ilac> Ilaclar => Set<Ilac>();
    public DbSet<StokHareketi> StokHareketleri => Set<StokHareketi>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ilac>()
            .HasIndex(x => x.IlacAdi)
            .IsUnique();

        modelBuilder.Entity<Ilac>()
            .HasIndex(x => x.Barkod)
            .IsUnique()
            .HasFilter("[Barkod] IS NOT NULL");

        modelBuilder.Entity<StokHareketi>()
            .HasIndex(x => x.Tarih);

        modelBuilder.Entity<Ilac>()
            .Property(x => x.GuncellemeTarihi)
            .HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
