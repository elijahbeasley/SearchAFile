using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights;
using SearchAFile.Web.Extensions;
using SearchAFile.Helpers;

namespace SearchAFile;

/// <summary>
/// Provides various utility methods related to system functionality.
/// </summary>
public static class SystemFunctions
{
    private const string DefaultDashboardURL = "~/Home/LogIn";
    public const string EmptyImageSource = "data:image/gif;base64,R0lGODlhAQABAPcAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACH5BAEAAP8ALAAAAAABAAEAAAgEAP8FBAA7";

    public static IConfiguration Configuration { get; set; }

    /// <summary>
    /// Gets the appropriate dashboard URL based on the user's role.
    /// </summary>
    /// <param name="role">The role of the user.</param>
    /// <returns>The URL of the corresponding dashboard.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the role is null or empty.</exception>
    public static string GetDashboardURL(string role)
    {
        if (string.IsNullOrEmpty(role))
            throw new ArgumentNullException(nameof(role), "Role cannot be null or empty");

        return role switch
        {
            "System Admin" => "~/SystemAdmins/Dashboard",
            "Admin" => "~/Admins/Dashboard",
            "Employee" => "~/Employees/Dashboard",
            _ => DefaultDashboardURL,
        };
    }

    /// <summary>
    /// Sets a message and its associated color in the current session.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <exception cref="InvalidOperationException">Thrown if the session is not available.</exception>
    public static void SetMessage(string message)
    {
        if (HttpContextHelper.Current?.Session == null)
            throw new InvalidOperationException("Session is not available");

        var session = HttpContextHelper.Current.Session;
        var newMessage = session.GetString("NewMessage");
        var newMessageColor = session.GetString("NewMessageColor");

        session.SetString("Message", newMessage ?? message);
        session.SetString("MessageColor", newMessageColor ?? "default");

        if (!string.IsNullOrEmpty(newMessage)) session.Remove("NewMessage");
        if (!string.IsNullOrEmpty(newMessageColor)) session.Remove("NewMessageColor");
    }

    /// <summary>
    /// Converts a column index to a corresponding letter(s), similar to Excel column naming.
    /// </summary>
    /// <param name="index">The index of the column (starting from 1).</param>
    /// <returns>The corresponding column letter(s).</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is less than or equal to 0.</exception>
    public static string GetLetterFromIndex(int index)
    {
        if (index <= 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Index must be greater than 0");

        const int columnBase = 26;
        const int maxDigitCount = 7;  // ceil(log26(Int32.Max))

        var result = new StringBuilder(maxDigitCount);
        while (index > 0)
        {
            index--; // Adjust for zero-based index
            result.Insert(0, (char)('A' + (index % columnBase)));
            index /= columnBase;
        }

        return result.ToString();
    }

    /// <summary>
    /// Creates a substring of a given string, truncating it if it exceeds the specified length, and adding ellipsis if necessary.
    /// </summary>
    /// <param name="maxLength">The maximum allowed length of the substring.</param>
    /// <param name="input">The string to truncate.</param>
    /// <returns>A substring of the input string.</returns>
    public static string CreateSubstring(int maxLength, string input)
    {
        if (string.IsNullOrEmpty(input)) return "";

        var words = input.Split(' ');
        var sb = new StringBuilder();
        int characterCount = 0;

        foreach (var word in words)
        {
            characterCount += word.Length + 1;
            if (characterCount <= maxLength - 3)
                sb.Append(word + " ");
            else
            {
                sb.Append("...");
                break;
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Generates a random string based on a set of allowed characters and the desired length.
    /// </summary>
    /// <param name="characters">The set of characters from which the string is generated.</param>
    /// <param name="length">The length of the generated string.</param>
    /// <returns>A randomly generated string.</returns>
    /// <exception cref="ArgumentException">Thrown if the character set is empty or length is less than or equal to 0.</exception>
    public static string GenerateRandomString(string characters, int length)
    {
        if (string.IsNullOrEmpty(characters) || length <= 0)
            throw new ArgumentException("Character set cannot be empty and length must be greater than 0.");

        var random = new Random();
        var randomString = new StringBuilder(length);

        for (int i = 0; i < length; i++)
        {
            randomString.Append(characters[random.Next(characters.Length)]);
        }

        return randomString.ToString();
    }

    public static string GetDayAbbreviation(DayOfWeek day)
    {
        switch (day)
        {
            case DayOfWeek.Sunday: return "Sun";
            case DayOfWeek.Monday: return "Mon";
            case DayOfWeek.Tuesday: return "Tue";
            case DayOfWeek.Wednesday: return "Wed";
            case DayOfWeek.Thursday: return "Thu";
            case DayOfWeek.Friday: return "Fri";
            case DayOfWeek.Saturday: return "Sat";
            default: throw new ArgumentOutOfRangeException();
        }
    }

}