using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PRN231_SU25_SE171121_DAL.Entities;

namespace PRN231_SU25_SE171121_DAL;

public partial class Su25leopardDbContext : DbContext
{
    public Su25leopardDbContext()
    {
    }

    public Su25leopardDbContext(DbContextOptions<Su25leopardDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<LeopardAccount> LeopardAccounts { get; set; }

    public virtual DbSet<LeopardProfile> LeopardProfiles { get; set; }

    public virtual DbSet<LeopardType> LeopardTypes { get; set; }

    private string GetConnectionString()
    {
        IConfiguration config = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", true, true)
                    .Build();
        var strConn = config["ConnectionStrings:DefaultConnectionStringDB"];

        return strConn;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LeopardAccount>(entity =>
        {
            entity.HasKey(e => e.AccountId);

            entity.ToTable("LeopardAccount");

            entity.Property(e => e.AccountId).HasColumnName("AccountID");
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.UserName).HasMaxLength(50);
        });

        modelBuilder.Entity<LeopardProfile>(entity =>
        {
            entity.ToTable("LeopardProfile");

            entity.Property(e => e.CareNeeds).HasMaxLength(1500);
            entity.Property(e => e.Characteristics).HasMaxLength(2000);
            entity.Property(e => e.LeopardName).HasMaxLength(150);
            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");

            entity.HasOne(d => d.LeopardType).WithMany(p => p.LeopardProfiles)
                .HasForeignKey(d => d.LeopardTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LeopardProfile_LeopardType");
        });

        modelBuilder.Entity<LeopardType>(entity =>
        {
            entity.ToTable("LeopardType");

            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.LeopardTypeName).HasMaxLength(250);
            entity.Property(e => e.Origin).HasMaxLength(250);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
