using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SearchAFile.Core.Domain.Entities;

[Table("Collection")]
public partial class Collection
{
    [Key]
    [Column("CollectionID")]
    public Guid CollectionId { get; set; }

    [Column("CompanyID")]
    [DisplayName("Company")]
    [Required(ErrorMessage = "Company is required.")]
    public Guid? CompanyId { get; set; }

    [Column("Collection")]
    [DisplayName("Collection")]
    [Required(ErrorMessage = "Collection is required.")]
    [StringLength(50)]
    public string? Collection1 { get; set; }

    [Column("OpenAIAssistantID")]
    [DisplayName("Assistant ID")]
    [StringLength(50)]
    public string? OpenAiAssistantId { get; set; }

    [Column("OpenAIVectorStoreID")]
    [DisplayName("Vector Store ID")]
    [StringLength(50)]
    public string? OpenAiVectorStoreId { get; set; }

    [Column("ImageURL")]
    [DisplayName("Image")]
    [Required(ErrorMessage = "Image is required.")]
    [StringLength(50)]
    public string? ImageUrl { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? Created { get; set; }

    [Column("CreatedByUserID")]
    [DisplayName("Created By")]
    public Guid CreatedByUserId { get; set; }

    [DisplayName("Public/Private")]

    [Required(ErrorMessage = "Public/private is required.")]
    public bool Private { get; set; }

    [DisplayName("Sort Order")]
    public int? SortOrder { get; set; }

    [DisplayName("Status")]
    [Required(ErrorMessage = "Status is required.")]
    public bool Active { get; set; }

    [ForeignKey("CompanyId")]
    [InverseProperty("Collections")]
    public virtual Company? Company { get; set; }

    [ForeignKey("CreatedByUserId")]
    [InverseProperty("Collections")]
    public virtual User? CreatedByUser { get; set; }

    [NotMapped]
    public string? CreatedByUserFullName { get; set; }

    [InverseProperty("Collection")]
    public virtual ICollection<File> Files { get; set; } = new List<File>();

    [NotMapped]
    public int? FilesCount { get; set; }
}
