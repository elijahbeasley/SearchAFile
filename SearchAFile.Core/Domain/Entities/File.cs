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

    [Column("FileGroupID")]
    public Guid? FileGroupId { get; set; }

    [Column("File")]
    [DisplayName("File Name")]
    [Required(ErrorMessage = "File name is required.")]
    [StringLength(50)]
    public string? File1 { get; set; }
    [StringLength(50)]
    public string? Path { get; set; }

    [Column("OpenAIFileID")]
    [StringLength(50)]
    public string? OpenAIFileId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? Uploaded { get; set; }

    [Column("UploadedByUserID")]
    public Guid? UploadedByUserId { get; set; }

    [ForeignKey("FileGroupId")]
    [InverseProperty("Files")]
    public virtual FileGroup? FileGroup { get; set; }

    [ForeignKey("UploadedByUserId")]
    [InverseProperty("Files")]
    public virtual User? UploadedByUser { get; set; }
}
