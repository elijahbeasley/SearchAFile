using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SearchAFile.Core.Domain.Entities;

[Table("Company")]
public partial class Company
{
    [Key]
    [Column("CompanyID")]
    public Guid CompanyId { get; set; }

    [Column("Company")]
    [StringLength(50)]
    public string? Company1 { get; set; }

    [StringLength(50)]
    public string? ContactName { get; set; }

    [StringLength(50)]
    public string? ContactEmailAddress { get; set; }

    [StringLength(20)]
    public string? ContactPhoneNumber { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [Required]
    [StringLength(30)]
    public required string HeaderLogo { get; set; }

    [Required]
    [StringLength(30)]
    public required string FooterLogo { get; set; }

    [Required]
    [StringLength(30)]
    public required string EmailLogo { get; set; }

    [Required]
    [StringLength(50)]
    public required string PrimaryColor { get; set; }

    [Required]
    [StringLength(50)]
    public required string SecondaryColor { get; set; }

    [Required]
    [StringLength(50)]
    public required string PrimaryTextColor { get; set; }

    [Required]
    [StringLength(50)]
    public required string SecondaryTextColor { get; set; }

    [Column("URL")]
    [Required]
    [StringLength(500)]
    public required string Url { get; set; }

    public bool? Lock { get; set; }

    public bool? Active { get; set; }

    [InverseProperty("Company")]
    public virtual ICollection<FileGroup> FileGroups { get; set; } = new List<FileGroup>();

    [InverseProperty("Company")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
