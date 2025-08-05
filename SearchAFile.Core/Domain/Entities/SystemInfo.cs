using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SearchAFile.Core.Domain.Entities;

[Table("SystemInfo")]
public partial class SystemInfo
{
    [Key]
    [Column("SystemInfoID")]
    public Guid SystemInfoId { get; set; }

    [DisplayName("System Name")]
    [Required]
    [StringLength(50)]
    public required string SystemName { get; set; }

    [DisplayName("Contact Name")]
    [Required]
    [StringLength(50)]
    public required string ContactName { get; set; }

    [DisplayName("Contact Email Address")]
    [Required]
    [StringLength(50)]
    public required string ContactEmailAddress { get; set; }

    [DisplayName("Contact Phone Number")]
    [Required]
    [StringLength(20)]
    public required string ContactPhoneNumber { get; set; }

    [Required]
    [StringLength(50)]
    public required string Favicon { get; set; }
    
    [DisplayName("Header Logo")]
    [Required]
    [StringLength(50)]
    public required string HeaderLogo { get; set; }

    [DisplayName("Footer Logo")]
    [Required]
    [StringLength(50)]
    public required string FooterLogo { get; set; }

    [DisplayName("Email Logo")]
    [Required]
    [StringLength(50)]
    public required string EmailLogo { get; set; }

    [DisplayName("Primary Color")]
    [Required]
    [StringLength(50)]
    public required string PrimaryColor { get; set; }

    [DisplayName("Secondary Color")]
    [Required]
    [StringLength(50)]
    public required string SecondaryColor { get; set; }

    [DisplayName("Primary Text Color")]
    [Required]
    [StringLength(50)]
    public required string PrimaryTextColor { get; set; }

    [DisplayName("Secondary Text Color")]
    [Required]
    [StringLength(50)]
    public required string SecondaryTextColor { get; set; }

    [Column("URL")]
    [Required]
    [StringLength(500)]
    public required string Url { get; set; }

    [StringLength(10)]
    public string? Version { get; set; }
}
