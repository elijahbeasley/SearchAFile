using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using File = SearchAFile.Core.Domain.Entities.File;

namespace SearchAFile.Web.Helpers;

public class FileNameHelper
{
    public static string TruncateFileName(string name, int max)
    {
        if (max <= 0 || string.IsNullOrEmpty(name) || name.Length <= max) return name;
        var idx = name.LastIndexOf('.');
        var baseName = (idx > 0) ? name.Substring(0, idx) : name;
        var ext = (idx > 0) ? name.Substring(idx) : string.Empty;
        const string ell = "…";
        if (ext.Length + 1 > max) return ell + ext.Substring(Math.Max(0, ext.Length - (max - 1)));
        var room = max - ext.Length - ell.Length;
        if (room < 1) room = 1;
        var safeBase = baseName.Substring(0, Math.Min(room, baseName.Length));
        return safeBase + ell + ext;
    }
}