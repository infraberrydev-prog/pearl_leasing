using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using pearl_leasing.Models;

namespace pearl_leasing.Data;

public partial class PearlLeasingDbContext : DbContext
{
    public PearlLeasingDbContext()
    {
    }

    public PearlLeasingDbContext(DbContextOptions<PearlLeasingDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserLogin> UserLogins { get; set; }
    public DbSet<UserOTP> UserOtps { get; set; }
    public DbSet<LeasingReport> LeasingReports { get; set; }
    public DbSet<LeasingReportRow> LeasingReportRows { get; set; }
    public DbSet<Building> Buildings { get; set; }
    public DbSet<StallUnit> StallUnits { get; set; }
    public DbSet<VendorCategory> VendorCategories { get; set; }
    public DbSet<ExpensesReport> ExpenseReports { get; set; }
    public DbSet<AuditLogs> AuditLogs { get; set; }
    public DbSet<Logs_DeletedReports> Logs_DeletedReports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Roles_pkey");

            entity.HasIndex(e => e.Name, "Roles_Name_key").IsUnique();

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Users_pkey");

            entity.HasIndex(e => e.Email, "Users_Email_key").IsUnique();

            entity.HasIndex(e => e.Username, "Users_Username_key").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserLogin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("UserLogins_pkey");

            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.LoginTime)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.User).WithMany(p => p.UserLogins).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<UserOTP>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("UserOtps_pkey");

            entity.Property(e => e.ExpiryTime).HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.User)
                .WithMany(p => p.UserOTPs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_UserOtps_Users_UserId");
        });

        modelBuilder.Entity<LeasingReportRow>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("LeasingReportRows_pkey");

            // entity.Property(e => e.BuildingType).HasMaxLength(100);
            // entity.Property(e => e.StallUnit).HasMaxLength(50);

            entity.Property(e => e.MonthlyRental).HasColumnType("numeric");
            entity.Property(e => e.Electricity).HasColumnType("numeric");
            entity.Property(e => e.Water).HasColumnType("numeric");
            entity.Property(e => e.OtherUtilities).HasColumnType("numeric");
            entity.Property(e => e.PenaltyTenPercent).HasColumnType("numeric");
            entity.Property(e => e.RowTotalAmount).HasColumnType("numeric");

            entity.HasOne(d => d.LeasingReport)
                .WithMany(p => p.Rows)
                .HasForeignKey(d => d.LeasingReportId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_LeasingReportRows_LeasingReports_LeasingReportId");

            //entity.HasOne(d => d.Building)
            //    .WithMany()
            //    .HasForeignKey(d => d.BuildingId)
            //    .OnDelete(DeleteBehavior.SetNull)
            //    .HasConstraintName("FK_LeasingReportRows_Buildings_BuildingId");

            //entity.HasOne(d => d.StallUnit)
            //    .WithMany()
            //    .HasForeignKey(d => d.StallUnitId)
            //    .OnDelete(DeleteBehavior.SetNull)
            //    .HasConstraintName("FK_LeasingReportRows_StallUnits_StallUnitId");
        });

        OnModelCreatingPartial(modelBuilder);
    }
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
