using System.Text;
using System.Text.Json;
using System.Xml;

namespace SearchAFile.Web.Extensions;

/// <summary>
/// Provides extension methods for working with session variables.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Evaluates a nullable boolean, returning <c>false</c> if <c>null</c>, or its value otherwise.
    /// </summary>
    /// <param name="value">The nullable boolean to evaluate.</param>
    /// <returns>
    /// <c>true</c> if the value is <c>true</c>; 
    /// <c>false</c> if the value is <c>false</c> or <c>null</c>.
    /// </returns>
    /// <exception cref="Exception">Thrown if an unexpected error occurs during evaluation.</exception>
    public static bool IsNullOrDefault(this bool? value)
    {
        try
        {
            return value ?? false;
        }
        catch (Exception ex)
        {
            throw new Exception("An error occurred while evaluating IsNullOrDefault for a nullable boolean.", ex);
        }
    }

    /// <summary>
    /// Attempts to parse a string as a boolean and evaluates it, treating null, empty, whitespace, or invalid strings as <c>false</c>.
    /// </summary>
    /// <param name="value">The string to parse and evaluate.</param>
    /// <returns>
    /// <c>true</c> if the parsed value is <c>true</c>; 
    /// <c>false</c> if the parsed value is <c>false</c>, empty, whitespace, or parsing fails.
    /// </returns>
    /// <exception cref="Exception">Thrown if an unexpected error occurs during evaluation.</exception>
    public static bool IsNullOrDefault(this string value)
    {
        try
        {
            // Check for null, empty, or whitespace strings first
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            // Attempt to parse; if parsing fails, return false
            if (bool.TryParse(value, out var parsedValue))
            {
                return parsedValue;
            }

            return false;
        }
        catch (Exception ex)
        {
            throw new Exception("An error occurred while evaluating IsNullOrDefault for a string representing a boolean.", ex);
        }
    }

    /// <summary>
    /// Determines whether the specified nullable integer is either <c>null</c> or equal to 0.
    /// </summary>
    /// <param name="value">The nullable integer value to check.</param>
    /// <returns><c>true</c> if <c>null</c> or 0; otherwise, <c>false</c>.</returns>
    public static bool IsNullOrZero(this int? value)
    {
        try
        {
            return value == null || value == 0;
        }
        catch
        {
            throw;
        }
    }

    /// <summary>
    /// Determines whether the specified non-nullable integer is equal to 0.
    /// </summary>
    /// <param name="value">The integer value to check.</param>
    /// <returns><c>true</c> if 0; otherwise, <c>false</c>.</returns>
    public static bool IsNullOrZero(this int value)
    {
        try
        {
            return value == 0;
        }
        catch
        {
            throw;
        }
    }

    /// <summary>
    /// Sets an object in the session as a serialized JSON string.
    /// </summary>
    /// <param name="session">The current session.</param>
    /// <param name="key">The key to store the object under.</param>
    /// <param name="value">The object to store in the session.</param>
    public static void SetObject(this ISession session, string key, object value)
    {
        try
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }
        catch (Exception ex)
        {
            // Log or handle the error as needed
            throw new InvalidOperationException("Error setting object in session.", ex);
        }
    }

    /// <summary>
    /// Gets a deserialized object from the session.
    /// </summary>
    /// <typeparam name="T">The type of the object to retrieve.</typeparam>
    /// <param name="session">The current session.</param>
    /// <param name="key">The key of the object to retrieve.</param>
    /// <returns>The deserialized object, or the default value if not found.</returns>
    public static T GetObject<T>(this ISession session, string key)
    {
        try
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
        catch (Exception ex)
        {
            // Log or handle the error as needed
            throw new InvalidOperationException("Error retrieving object from session.", ex);
        }
    }

    /// <summary>
    /// Sets a boolean value in the session.
    /// </summary>
    /// <param name="session">The current session.</param>
    /// <param name="key">The key to store the boolean value under.</param>
    /// <param name="value">The boolean value to store in the session.</param>
    public static void SetBoolean(this ISession session, string key, bool value)
    {
        try
        {
            session.Set(key, BitConverter.GetBytes(value));
        }
        catch (Exception ex)
        {
            // Log or handle the error as needed
            throw new InvalidOperationException("Error setting boolean in session.", ex);
        }
    }

    /// <summary>
    /// Gets a boolean value from the session.
    /// </summary>
    /// <param name="session">The current session.</param>
    /// <param name="key">The key of the boolean value to retrieve.</param>
    /// <returns>The boolean value, or null if not found.</returns>
    public static bool? GetBoolean(this ISession session, string key)
    {
        try
        {
            var data = session.Get(key);
            if (data == null)
            {
                return null;
            }
            return BitConverter.ToBoolean(data, 0);
        }
        catch (Exception ex)
        {
            // Log or handle the error as needed
            throw new InvalidOperationException("Error retrieving boolean from session.", ex);
        }
    }

    /// <summary>
    /// Sets a double value in the session.
    /// </summary>
    /// <param name="session">The current session.</param>
    /// <param name="key">The key to store the double value under.</param>
    /// <param name="value">The double value to store in the session.</param>
    public static void SetDouble(this ISession session, string key, double value)
    {
        try
        {
            session.Set(key, BitConverter.GetBytes(value));
        }
        catch (Exception ex)
        {
            // Log or handle the error as needed
            throw new InvalidOperationException("Error setting double in session.", ex);
        }
    }

    /// <summary>
    /// Gets a double value from the session.
    /// </summary>
    /// <param name="session">The current session.</param>
    /// <param name="key">The key of the double value to retrieve.</param>
    /// <returns>The double value, or null if not found.</returns>
    public static double? GetDouble(this ISession session, string key)
    {
        try
        {
            var data = session.Get(key);
            if (data == null)
            {
                return null;
            }
            return BitConverter.ToDouble(data, 0);
        }
        catch (Exception ex)
        {
            // Log or handle the error as needed
            throw new InvalidOperationException("Error retrieving double from session.", ex);
        }
    }
}

/// <summary>
/// Provides extension methods for working with cookies.
/// </summary>
public static class CookieExtensions
{
    // Store a reference to IHttpContextAccessor for obtaining the current HttpContext
    private static readonly IHttpContextAccessor _httpContextAccessor = new HttpContextAccessor();

    /// <summary>
    /// The default cookie options, with a 7-day expiration, HttpOnly, and Secure flag set.
    /// </summary>
    private static readonly CookieOptions DefaultCookieOptions = new CookieOptions
    {
        Expires = DateTimeOffset.Now.AddDays(7),
        HttpOnly = true,
        Secure = true
    };

    private static HttpContext CurrentHttpContext => _httpContextAccessor.HttpContext;

    /// <summary>
    /// Sets an object as a serialized JSON string in a cookie.
    /// </summary>
    /// <typeparam name="T">The type of the object to store in the cookie.</typeparam>
    /// <param name="key">The key to store the object under in the cookie.</param>
    /// <param name="value">The object to store in the cookie.</param>
    /// <param name="options">Optional cookie options. Uses default options if null.</param>
    public static void SetObject<T>(string key, T value, CookieOptions options = null)
    {
        try
        {
            key = CleanCookieKey(key);

            var httpContext = CurrentHttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
            var cookieOptions = options ?? DefaultCookieOptions;
            string jsonValue = JsonSerializer.Serialize(value);
            httpContext.Response.Cookies.Append(key, jsonValue, cookieOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error setting object in cookie.", ex);
        }
    }

    /// <summary>
    /// Gets a deserialized object from a cookie.
    /// </summary>
    /// <typeparam name="T">The type of the object to retrieve from the cookie.</typeparam>
    /// <param name="key">The key of the object in the cookie.</param>
    /// <returns>The deserialized object, or default if not found.</returns>
    public static T? GetObject<T>(string key)
    {
        try
        {
            key = CleanCookieKey(key);

            var httpContext = CurrentHttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
            if (httpContext.Request.Cookies.TryGetValue(key, out string jsonValue))
            {
                if (jsonValue == "undefined")
                {
                    return default;
                }
                return JsonSerializer.Deserialize<T>(jsonValue);
            }
            return default;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error retrieving object from cookie.", ex);
        }
    }

    /// <summary>
    /// Updates an existing object in the cookie with a new value.
    /// </summary>
    /// <typeparam name="T">The type of the object to store in the cookie.</typeparam>
    /// <param name="key">The key of the object in the cookie.</param>
    /// <param name="updatedValue">The updated object to store in the cookie.</param>
    /// <param name="options">Optional cookie options. Uses default options if null.</param>
    public static void UpdateObject<T>(string key, T updatedValue, CookieOptions options = null)
    {
        try
        {
            key = CleanCookieKey(key);

            var httpContext = CurrentHttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
            var cookieOptions = options ?? DefaultCookieOptions;
            SetObject(key, updatedValue, cookieOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error updating object in cookie.", ex);
        }
    }

    /// <summary>
    /// Checks if a cookie exists in the request, even if its value is null.
    /// </summary>
    /// <param name="cookieName">The name of the cookie to check for existence.</param>
    /// <returns>
    /// <c>true</c> if the cookie exists (even if its value is null), otherwise <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if the cookie name is null or empty.</exception>
    public static bool CookieExists(string key)
    {
        try
        {
            key = CleanCookieKey(key);

            // Validate input
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key), "Cookie name cannot be null or empty.");
            }

            // Retrieve the cookie from the request
            var cookie = CurrentHttpContext.Request.Cookies[key];

            // Check if the cookie exists, even if its value is null
            if (cookie != null)
            {
                // Cookie exists, even if its value is null or empty
                return true;
            }

            // Cookie does not exist
            return false;
        }
        catch (Exception ex)
        {
            // Log or handle the exception as needed
            throw new ApplicationException("An error occurred while checking if the cookie exists.", ex);
        }
    }


    /// <summary>
    /// Deletes a specified cookie.
    /// </summary>
    /// <param name="key">The key of the cookie to delete.</param>
    public static void DeleteCookie(string key)
    {
        try
        {
            key = CleanCookieKey(key);

            var httpContext = CurrentHttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
            httpContext.Response.Cookies.Delete(key);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error deleting cookie.", ex);
        }
    }

    /// <summary>
    /// Sets a string value in a cookie.
    /// </summary>
    /// <param name="key">The key to store the string value under in the cookie.</param>
    /// <param name="value">The string value to store in the cookie.</param>
    /// <param name="options">Optional cookie options. Uses default options if null.</param>
    public static void SetCookie(string key, string value, CookieOptions options = null)
    {
        try
        {
            key = CleanCookieKey(key);

            var httpContext = CurrentHttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
            var cookieOptions = options ?? DefaultCookieOptions;
            httpContext.Response.Cookies.Append(key, value, cookieOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error setting string in cookie.", ex);
        }
    }

    /// <summary>
    /// Gets a string value from a cookie.
    /// </summary>
    /// <param name="key">The key of the string value in the cookie.</param>
    /// <returns>The string value, or null if the cookie is not found.</returns>
    public static string GetCookie(string key)
    {
        try
        {
            key = CleanCookieKey(key);

            var httpContext = CurrentHttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
            if (httpContext.Request.Cookies.TryGetValue(key, out string value))
            {
                return value;
            }
            return null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error retrieving string from cookie.", ex);
        }
    }

    /// <summary>
    /// Updates an existing string value in the cookie with a new value.
    /// </summary>
    /// <param name="key">The key of the string value in the cookie.</param>
    /// <param name="updatedValue">The updated string value to store in the cookie.</param>
    /// <param name="options">Optional cookie options. Uses default options if null.</param>
    public static void UpdateCookie(string key, string updatedValue, CookieOptions options = null)
    {
        try
        {
            var httpContext = CurrentHttpContext ?? throw new InvalidOperationException("HttpContext is not available.");
            var cookieOptions = options ?? DefaultCookieOptions;
            SetCookie(key, updatedValue, cookieOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error updating string in cookie.", ex);
        }
    }

    /// <summary>
    /// Cleans a cookie key by removing any invalid characters.
    /// Valid characters are: A-Z, a-z, 0-9, '-', '_', and '.'.
    /// </summary>
    /// <param name="key">The cookie key to clean.</param>
    /// <returns>A cleaned version of the cookie key with only valid characters.</returns>
    /// <exception cref="ArgumentException">Thrown if the cookie key is null or empty.</exception>
    public static string CleanCookieKey(string key)
    {
        try
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Cookie key cannot be null or empty.");
            }

            // Define allowed characters
            var allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_.";
            var cleanedKey = new StringBuilder();

            foreach (var character in key)
            {
                // Only append characters that are in the allowed set
                if (allowedChars.Contains(character))
                {
                    cleanedKey.Append(character);
                }
            }

            return cleanedKey.ToString();
        }
        catch (ArgumentException ex)
        {
            // Log or handle specific exceptions
            throw new XmlException($"An error occurred while cleaning the cookie key: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            // General catch block for unexpected exceptions
            throw new XmlException("An unexpected error occurred while cleaning the cookie key.", ex);
        }
    }

    /// <summary>
    /// Escapes a string so that it can be safely used inside JavaScript string literals.
    /// </summary>
    /// <param name="input">The raw string to escape.</param>
    /// <returns>
    /// A string with special characters (e.g., quotes, backslashes, line breaks) escaped for JavaScript.
    /// </returns>
    /// <example>
    /// "O'Brien".EscapeJsString() returns "O\\'Brien"
    /// </example>
    public static string EscapeJsString(this string input)
    {
        try
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return input
                .Replace("\\", "\\\\")   // Escape backslashes
                .Replace("'", "\\'")     // Escape single quotes
                .Replace("\"", "\\\"")   // Escape double quotes
                .Replace("\r", "\\r")    // Escape carriage returns
                .Replace("\n", "\\n");   // Escape newlines
        }
        catch
        {
            throw;
        }
    }
}