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

    [StringLength(50)]
    public string? SystemName { get; set; }

    [StringLength(50)]
    public string? ContactName { get; set; }

    [StringLength(50)]
    public string? ContactEmailAddress { get; set; }

    [StringLength(20)]
    public string? ContactPhoneNumber { get; set; }

    [StringLength(30)]
    public string? Favicon { get; set; }

    [StringLength(30)]
    public string? HeaderLogo { get; set; }

    [StringLength(30)]
    public string? FooterLogo { get; set; }

    [StringLength(30)]
    public string? EmailLogo { get; set; }

    [StringLength(50)]
    public string? PrimaryColor { get; set; }

    [StringLength(50)]
    public string? SecondaryColor { get; set; }

    [StringLength(50)]
    public string? PrimaryTextColor { get; set; }

    [StringLength(50)]
    public string? SecondaryTextColor { get; set; }

    [Column("URL")]
    [StringLength(500)]
    public string? Url { get; set; }

    [StringLength(10)]
    public string? Version { get; set; }
}
