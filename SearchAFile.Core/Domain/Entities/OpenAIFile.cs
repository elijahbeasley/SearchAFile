using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchAFile.Core.Domain.Entities;
public class OpenAIFile
{
    public string Id { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Purpose { get; set; } = "";
    public long Bytes { get; set; }
    public DateTime CreatedAt { get; set; }
}