using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SearchAFile.Core.Domain.Entities;

namespace SearchAFile.Infrastructure.Data;

public partial class SearchAFileDbContext : DbContext
{
    public SearchAFileDbContext()
    {
    }

    public SearchAFileDbContext(DbContextOptions<SearchAFileDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Company> Companies { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<Core.Domain.Entities.File> Files { get; set; }

    public virtual DbSet<Collection> Collections { get; set; }

    public virtual DbSet<SystemInfo> SystemInfos { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("Company", tb => tb.HasTrigger("trg_Company_Audit"));

            entity.Property(e => e.CompanyId).HasDefaultValueSql("(newsequentialid())");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PK__Events__7944C870E8A858CA");

            entity.Property(e => e.EventId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.ChangeDate).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.ChangedByUser).WithMany(p => p.Events).HasConstraintName("FK_Event_User");
        });

        modelBuilder.Entity<Core.Domain.Entities.File>(entity =>
        {
            entity.HasKey(e => e.FileId).HasName("PK_File_1");

            entity.ToTable("File", tb => tb.HasTrigger("trg_File_Audit"));

            entity.Property(e => e.FileId).HasDefaultValueSql("(newsequentialid())");

            entity.HasOne(d => d.Collection).WithMany(p => p.Files).HasConstraintName("FK_File_Collection");

            entity.HasOne(d => d.UploadedByUser).WithMany(p => p.Files).HasConstraintName("FK_File_User");
        });

        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(e => e.CollectionId).HasName("PK_Collection_1");

            entity.ToTable("Collection", tb => tb.HasTrigger("trg_Collection_Audit"));

            entity.Property(e => e.CollectionId).HasDefaultValueSql("(newsequentialid())");

            entity.HasOne(d => d.Company).WithMany(p => p.Collections).HasConstraintName("FK_Collection_Company");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Collections).HasConstraintName("FK_Collection_User");
        });

        modelBuilder.Entity<SystemInfo>(entity =>
        {
            entity.HasKey(e => e.SystemInfoId).HasName("PK_SystemInfo_1");

            entity.ToTable("SystemInfo", tb => tb.HasTrigger("trg_SystemInfo_Audit"));

            entity.Property(e => e.SystemInfoId).HasDefaultValueSql("(newsequentialid())");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK_User_1");

            entity.ToTable("User", tb => tb.HasTrigger("trg_User_Audit"));

            entity.Property(e => e.UserId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.EmailVerified).HasDefaultValue(false);

            entity.HasOne(d => d.Company).WithMany(p => p.Users).HasConstraintName("FK_User_Company");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
