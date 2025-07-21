using System;
using System.Collections.Generic;
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
    public Guid? CompanyId { get; set; }

    [Column("FileGroup")]
    [StringLength(50)]
    public string? FileGroup1 { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? Created { get; set; }

    [Column("CreatedByUserID")]
    public Guid? CreatedByUserId { get; set; }

    public int? SortOrder { get; set; }

    public bool? Active { get; set; }

    [ForeignKey("CompanyId")]
    [InverseProperty("FileGroups")]
    public virtual Company? Company { get; set; }

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("FileGroups")]
    public virtual User? CreatedByUser { get; set; }

    [InverseProperty("FileGroup")]
    public virtual ICollection<File> Files { get; set; } = new List<File>();
}
