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
    [Required(ErrorMessage = "Company is required.")]
    public required Guid CompanyId { get; set; }

    [DisplayName("First Name")]
    [Required(ErrorMessage = "First name is required.")]
    [StringLength(25)]
    public required string FirstName { get; set; }

    [DisplayName("Last Name")]
    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(25)]
    public required string LastName { get; set; }

    public string FullName => $"{FirstName ?? ""} {LastName ?? ""}".Trim();

    public string FullNameReverse => $"{LastName ?? ""}, {FirstName ?? ""}".Trim();

    [DisplayName("Email Address")]
    [Required(ErrorMessage = "Email address is required.")]
    [RegularExpression(@"^([A-z0-9]|\.){2,}@[A-z0-9]{2,}.[A-z0-9]{2,}$", ErrorMessage = "Invalid email address entered. Please enter an email address in the format: 'aaa@bbb.ccc'.")]
    [StringLength(50)]
    public required string EmailAddress { get; set; }

    [DisplayName("Phone Number")]
    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^\(\d{3}\) \d{3}-\d{4}$", ErrorMessage = "Phone number must be in the format (###) ###-####.")]
    [StringLength(20)]
    public required string PhoneNumber { get; set; }

    [DisplayName("Current Password")]
    [RegularExpression(@"^.*(?=.{6,})(?=.*[a-z])(?=.*[A-Z])(?=.*\d).*$", ErrorMessage = "Invalid password entered. Password must be between 8 and 30 characters long, contain at least one upper case letter, at least one lower case letter, and at least one number.")]
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(200)]
    public required string Password { get; set; }

    [Column("ResetURL")]
    [StringLength(50)]
    public string? ResetUrl { get; set; }

    [Column("ResetPIN")]
    [StringLength(6)]
    public string? ResetPin { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ResetExpiration { get; set; }

    [DisplayName("Email Verified")]
    public bool EmailVerified { get; set; }

    [Column("EmailVerificationURL")]
    public Guid EmailVerificationUrl { get; set; }

    [StringLength(50)]
    [DisplayName("Headshot")]
    public string? HeadshotPath { get; set; }

    [Required(ErrorMessage = "Role is required.")]
    [StringLength(20)]
    public required string Role { get; set; }

    [DisplayName("Status")]
    [Required(ErrorMessage = "Status is required.")]
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
