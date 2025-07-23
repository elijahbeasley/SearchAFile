using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SearchAFile.Core.Domain.Entities;

[Table("User")]
public partial class User
{
    [Key]
    [Column("UserID")]
    public Guid UserId { get; set; }

    [Column("CompanyID")]
    [Required]
    public required Guid CompanyId { get; set; }

    [Required]
    [StringLength(25)]
    public required string FirstName { get; set; }

    [Required]
    [StringLength(25)]
    public required string LastName { get; set; }

    public string FullName => $"{FirstName ?? ""} {LastName ?? ""}".Trim();

    public string FullNameReverse => $"{LastName ?? ""}, {FirstName ?? ""}".Trim();

    [Required]
    [StringLength(50)]
    public required string EmailAddress { get; set; }

    [Required]
    [StringLength(20)]
    public required string PhoneNumber { get; set; }

    [Required]
    [StringLength(200)]
    public required string Password { get; set; }

    [Column("ResetURL")]
    [StringLength(30)]
    public string? ResetUrl { get; set; }

    [Column("ResetPIN")]
    [StringLength(6)]
    public string? ResetPin { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ResetExpiration { get; set; }

    public bool EmailVerified { get; set; }

    [Column("EmailVerificationURL")]
    public Guid EmailVerificationUrl { get; set; }

    [StringLength(30)]
    public string? HeadshotPath { get; set; }

    [Required]
    [StringLength(20)]
    public required string Role { get; set; }

    [DisplayName("Status")]
    [Required]
    public required bool Active { get; set; }

    [ForeignKey("CompanyId")]
    [InverseProperty("Users")]
    public virtual Company? Company { get; set; }

    [InverseProperty("ChangedByUser")]
    public virtual ICollection<Event> Events { get; set; } = new List<Event>();

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<FileGroup> FileGroups { get; set; } = new List<FileGroup>();

    [InverseProperty("UploadedByUser")]
    public virtual ICollection<File> Files { get; set; } = new List<File>();
}
