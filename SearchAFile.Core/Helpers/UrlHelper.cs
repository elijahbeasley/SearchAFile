using System;
using System.Linq;

namespace SearchAFile.Core.Helpers;

public static class UrlHelper
{
    public static string Combine(params string[] parts)
    {
        try
        {
            if (parts == null || parts.Length == 0)
            {
                throw new ArgumentException("At least one part must be provided.", nameof(parts));
            }

            // Check for null or empty strings in the array and throw an exception if any are found
            if (parts.Any(part => part == null))
            {
                throw new ArgumentException("None of the URL parts can be null.", nameof(parts));
            }

            // Check if the first part starts with "~/" and preserve it
            bool hasTilde = parts[0].StartsWith("~/");

            // Combine parts, trimming slashes and removing empty parts
            var combined = string.Join("/", parts
                .Select(p => p.Trim('/')) // Remove leading and trailing slashes
                .Where(p => !string.IsNullOrEmpty(p))); // Remove empty parts

            return hasTilde ? "~/" + combined : combined;
        }
        catch
        {
            throw; // Rethrow the exception, preserving the original stack trace
        }
    }
}
