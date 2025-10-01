using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchAFile.Core.Domain.Entities;

public class ChatMessage
{
    public string Role { get; set; } = "";  // "user", "assistant", "system"
    public string Text { get; set; } = "";  // plain text (no HTML)
    public DateTimeOffset? Timestamp { get; set; }
}