public static class ParsingHelpers
{
    public static int ToIntOrDefault(this string? input, int defaultValue = 0)
    {
        return int.TryParse(input, out var result) ? result : defaultValue;
    }
    public static string ToStringOrDefault(this string? input, string defaultValue = "")
    {
        return string.IsNullOrWhiteSpace(input) ? defaultValue : input;
    }
}