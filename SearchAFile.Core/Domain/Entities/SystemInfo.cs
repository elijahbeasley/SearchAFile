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
    [Required(ErrorMessage = "System name is required.")]
    [StringLength(50)]
    public required string SystemName { get; set; }

    [DisplayName("Contact Name")]
    [Required(ErrorMessage = "Contact name is required.")]
    [StringLength(50)]
    public required string ContactName { get; set; }

    [DisplayName("Contact Email Address")]
    [Required(ErrorMessage = "Contact email address is required.")]
    [StringLength(50)]
    public required string ContactEmailAddress { get; set; }

    [DisplayName("Contact Phone Number")]
    [Required(ErrorMessage = "Contact phone number is required.")]
    [StringLength(20)]
    public required string ContactPhoneNumber { get; set; }

    [Required(ErrorMessage = "Favicon is required.")]
    [StringLength(50)]
    public required string Favicon { get; set; }
    
    [DisplayName("Header Logo")]
    [Required(ErrorMessage = "Header logo is required.")]
    [StringLength(50)]
    public required string HeaderLogo { get; set; }

    [DisplayName("Footer Logo")]
    [Required(ErrorMessage = "Footer logo is required.")]
    [StringLength(50)]
    public required string FooterLogo { get; set; }

    [DisplayName("Email Logo")]
    [Required(ErrorMessage = "Email logo is required.")]
    [StringLength(50)]
    public required string EmailLogo { get; set; }

    [DisplayName("Primary Color")]
    [Required(ErrorMessage = "Primary color is required.")]
    [StringLength(50)]
    public required string PrimaryColor { get; set; }

    [DisplayName("Secondary Color")]
    [Required(ErrorMessage = "Secondary color is required.")]
    [StringLength(50)]
    public required string SecondaryColor { get; set; }

    [DisplayName("Primary Text Color")]
    [Required(ErrorMessage = "Primary text color is required.")]
    [StringLength(50)]
    public required string PrimaryTextColor { get; set; }

    [DisplayName("Secondary Text Color")]
    [Required(ErrorMessage = "Secondary text color is required.")]
    [StringLength(50)]
    public required string SecondaryTextColor { get; set; }

    [Column("URL")]
    [Required(ErrorMessage = "URL is required.")]
    [StringLength(500)]
    public required string Url { get; set; }

    [Required(ErrorMessage = "Version is required.")]
    [StringLength(10)]
    public string? Version { get; set; }
}
