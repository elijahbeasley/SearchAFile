using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SearchAFile.Core.Domain.Entities;

public partial class Event
{
    [Key]
    [Column("EventID")]
    public Guid EventId { get; set; }

    [StringLength(100)]
    public string? TableName { get; set; }

    [StringLength(10)]
    public string? ActionType { get; set; }

    [StringLength(100)]
    public string? PrimaryKeyValue { get; set; }

    public Guid? ChangedByUserId { get; set; }

    public DateTime? ChangeDate { get; set; }

    public string? Details { get; set; }

    [ForeignKey("ChangedByUserId")]
    [InverseProperty("Events")]
    public virtual User? ChangedByUser { get; set; }
}
