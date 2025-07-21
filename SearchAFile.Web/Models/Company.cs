using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SearchAFile.Web.Models;

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

    public bool? Lock { get; set; }

    public bool? Active { get; set; }

    [InverseProperty("Company")]
    public virtual ICollection<FileGroup> FileGroups { get; set; } = new List<FileGroup>();

    [InverseProperty("Company")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
