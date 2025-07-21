using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SearchAFile.Web.Models;

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

    public virtual DbSet<File> Files { get; set; }

    public virtual DbSet<FileGroup> FileGroups { get; set; }

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
        });

        modelBuilder.Entity<File>(entity =>
        {
            entity.HasKey(e => e.FileId).HasName("PK_File_1");

            entity.ToTable("File", tb => tb.HasTrigger("trg_File_Audit"));

            entity.Property(e => e.FileId).HasDefaultValueSql("(newsequentialid())");

            entity.HasOne(d => d.FileGroup).WithMany(p => p.Files).HasConstraintName("FK_File_FileGroup");

            entity.HasOne(d => d.UploadedByUser).WithMany(p => p.Files).HasConstraintName("FK_File_User");
        });

        modelBuilder.Entity<FileGroup>(entity =>
        {
            entity.HasKey(e => e.FileGroupId).HasName("PK_FileGroup_1");

            entity.ToTable("FileGroup", tb => tb.HasTrigger("trg_FileGroup_Audit"));

            entity.Property(e => e.FileGroupId).HasDefaultValueSql("(newsequentialid())");

            entity.HasOne(d => d.Company).WithMany(p => p.FileGroups).HasConstraintName("FK_FileGroup_Company");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.FileGroups).HasConstraintName("FK_FileGroup_User");
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
