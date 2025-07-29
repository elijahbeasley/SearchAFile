using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SearchAFile.Core.Domain.Entities;

[Table("FileGroup")]
public partial class FileGroup
{
    [Key]
    [Column("FileGroupID")]
    public Guid FileGroupId { get; set; }

    [Column("CompanyID")]
    [DisplayName("Company")]
    [Required(ErrorMessage = "Company is required.")]
    public Guid? CompanyId { get; set; }

    [Column("FileGroup")]
    [DisplayName("File Group")]
    [Required(ErrorMessage = "File group is required.")]
    [StringLength(50)]
    public string? FileGroup1 { get; set; }

    [Column("ImageURL")]
    [DisplayName("Image")]
    [StringLength(50)]
    public string? ImageUrl { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? Created { get; set; }

    [Column("CreatedByUserID")]
    [DisplayName("Created By")]
    public Guid CreatedByUserId { get; set; }

    [DisplayName("Sort Order")]
    public int? SortOrder { get; set; }

    [DisplayName("Status")]
    [Required(ErrorMessage = "Status is required.")]
    public bool Active { get; set; }

    [ForeignKey("CompanyId")]
    [InverseProperty("FileGroups")]
    public virtual Company? Company { get; set; }

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("FileGroups")]
    public virtual User? CreatedByUser { get; set; }

    [NotMapped]
    public string? CreatedByUserFullName { get; set; }

    [InverseProperty("FileGroup")]
    public virtual ICollection<File> Files { get; set; } = new List<File>();

    [NotMapped]
    public int? FilesCount { get; set; }
}
