using System;
using System.Collections.Generic;
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

    [Required]
    [StringLength(50)]
    public required string SystemName { get; set; }

    [Required]
    [StringLength(50)]
    public required string ContactName { get; set; }

    [Required]
    [StringLength(50)]
    public required string ContactEmailAddress { get; set; }

    [Required]
    [StringLength(20)]
    public required string ContactPhoneNumber { get; set; }

    [Required]
    [StringLength(50)]
    public required string Favicon { get; set; }

    [Required]
    [StringLength(50)]
    public required string HeaderLogo { get; set; }

    [Required]
    [StringLength(50)]
    public required string FooterLogo { get; set; }

    [Required]
    [StringLength(50)]
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

    [StringLength(10)]
    public string? Version { get; set; }
}
