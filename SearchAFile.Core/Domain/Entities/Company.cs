using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    [DisplayName("Company")]
    [Required(ErrorMessage = "Company is required.")]
    [StringLength(50)]
    public string? Company1 { get; set; }

    [DisplayName("Contact Name")]
    [Required(ErrorMessage = "Contact name is required.")]
    [StringLength(50)]
    public string? ContactName { get; set; }

    [DisplayName("Contact Email Address")]
    [Required(ErrorMessage = "Contact email address is required.")]
    [RegularExpression(@"^([A-z0-9]|\.){2,}@[A-z0-9]{2,}.[A-z0-9]{2,}$", ErrorMessage = "Invalid contact email address entered. Please enter a contact email address in the format: 'aaa@bbb.ccc'.")]
    [StringLength(50)]
    public string? ContactEmailAddress { get; set; }

    [DisplayName("Contact Phone Number")]
    [Required(ErrorMessage = "Contact phone number is required.")]
    [RegularExpression(@"^\(\d{3}\) \d{3}-\d{4}$", ErrorMessage = "Contact phone number must be in the format (###) ###-####.")]
    [StringLength(20)]
    public string? ContactPhoneNumber { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [DisplayName("Header Logo")]
    //[Required(ErrorMessage = "Header logo is required.")]
    [StringLength(50)]
    public required string HeaderLogo { get; set; }

    [DisplayName("Footer Logo")]
    //[Required(ErrorMessage = "Footer logo is required.")]
    [StringLength(50)]
    public required string FooterLogo { get; set; }

    [DisplayName("Email Logo")]
    //[Required(ErrorMessage = "Email logo is required.")]
    [StringLength(50)]
    public required string EmailLogo { get; set; }

    [DisplayName("Primary Color")]
    [Required(ErrorMessage = "Primary color is required.")]
    [StringLength(50)]
    public required string? PrimaryColor { get; set; }

    [DisplayName("Secondary Color")]
    [Required(ErrorMessage = "Secondary color is required.")]
    [StringLength(50)]
    public required string? SecondaryColor { get; set; }

    [DisplayName("Primary Text Color")]
    [Required(ErrorMessage = "Primary text color is required.")]
    [StringLength(50)]
    public required string? PrimaryTextColor { get; set; }

    [DisplayName("Secondary Text Color")]
    [Required(ErrorMessage = "Secondary text color is required.")]
    [StringLength(50)]
    public required string? SecondaryTextColor { get; set; }

    [DisplayName("URL")]
    [Column("URL")]
    [RegularExpression(@"^(https?:\/\/)?(www\.)?([a-zA-Z0-9\-]+\.)+[a-zA-Z]{2,}(:\d+)?(\/\S*)?$", ErrorMessage = "Please enter a valid URL.")]
    [Required(ErrorMessage = "Url is required.")]
    [StringLength(500)]
    public required string? Url { get; set; }

    [DisplayName("Locked")]
    [Required(ErrorMessage = "Locked is required.")]
    public bool Lock { get; set; }

    [DisplayName("Status")]
    [Required(ErrorMessage = "Status is required.")]
    public bool Active { get; set; }

    [InverseProperty("Company")]
    public virtual ICollection<FileGroup> FileGroups { get; set; } = new List<FileGroup>();

    [InverseProperty("Company")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
