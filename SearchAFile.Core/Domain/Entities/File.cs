using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SearchAFile.Core.Domain.Entities;

[Table("File")]
public partial class File
{
    [Key]
    [Column("FileID")]
    public Guid FileId { get; set; }

    [Column("CollectionID")]
    public Guid? CollectionId { get; set; }

    [Column("File")]
    [DisplayName("File Name")]
    [Required(ErrorMessage = "File name is required.")]
    [StringLength(50)]
    public string? File1 { get; set; }

    [StringLength(10)]
    public string? Extension { get; set; }
    public string Path => $"{FileId}.{Extension ?? ""}".Trim();

    [Column("OpenAIFileID")]
    [StringLength(50)]
    public string? OpenAIFileId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? Uploaded { get; set; }

    [Column("UploadedByUserID")]
    public Guid? UploadedByUserId { get; set; }

    [ForeignKey("CollectionId")]
    [InverseProperty("Files")]
    public virtual Collection? Collection { get; set; }

    [ForeignKey("UploadedByUserId")]
    [InverseProperty("Files")]
    public virtual User? UploadedByUser { get; set; }
}
