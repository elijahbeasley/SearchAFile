using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SearchAFile.Web.Models;

[Table("User")]
public partial class User
{
    [Key]
    [Column("UserID")]
    public Guid UserId { get; set; }

    [Column("CompanyID")]
    public Guid? CompanyId { get; set; }

    [StringLength(25)]
    public string? FirstName { get; set; }

    [StringLength(25)]
    public string? LastName { get; set; }

    [StringLength(50)]
    public string? EmailAddress { get; set; }

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(200)]
    public string? Password { get; set; }

    [Column("ResetURL")]
    [StringLength(30)]
    public string? ResetUrl { get; set; }

    [Column("ResetPIN")]
    [StringLength(6)]
    public string? ResetPin { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ResetExpiration { get; set; }

    public bool? EmailVerified { get; set; }

    [Column("EmailVerificationURL")]
    [StringLength(50)]
    public string? EmailVerificationUrl { get; set; }

    [StringLength(30)]
    public string? HeadshotPath { get; set; }

    [StringLength(20)]
    public string? Role { get; set; }

    public bool? Active { get; set; }

    [ForeignKey("CompanyId")]
    [InverseProperty("Users")]
    public virtual Company? Company { get; set; }

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<FileGroup> FileGroups { get; set; } = new List<FileGroup>();

    [InverseProperty("UploadedByUser")]
    public virtual ICollection<File> Files { get; set; } = new List<File>();
}
