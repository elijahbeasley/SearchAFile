namespace SearchAFile.Core.Helpers;

public static class PhoneNumberHelper
{
    /// <summary>
    /// Cleans a phone number by removing all non-digit characters and standardizing to 10-digit format.
    /// </summary>
    /// <param name="input">The raw phone number string.</param>
    /// <returns>The cleaned 10-digit phone number, or null if invalid.</returns>
    public static string? CleanPhoneNumber(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        // Remove everything that is not a digit
        var digits = new string(input.Where(char.IsDigit).ToArray());

        // Remove leading country code '1' if present and result is 11 digits
        if (digits.Length == 11 && digits.StartsWith("1"))
            digits = digits.Substring(1);

        // Ensure exactly 10 digits
        return digits.Length == 10 ? digits : null;
    }
}