using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text;

namespace SearchAFile.Web.Helpers;

public static class ApiErrorHelper
{
    /// <summary>Returns a readable string for an ApiResult error, including model validation.</summary>
    public static string GetErrorString<T>(ApiResult<T> result)
    {
        if (result == null)
            return "An unknown error occurred.";

        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            sb.AppendLine(result.ErrorMessage);

        if (result.Errors != null)
        {
            foreach (var (field, messages) in result.Errors)
            {
                foreach (var msg in messages)
                    sb.AppendLine($"{field}: {msg}");
            }
        }

        return sb.Length > 0 ? sb.ToString().Trim() : "An error occurred.";
    }

    public static void AddErrorsToModelState<T>(ApiResult<T> result, ModelStateDictionary modelState, string? modelPrefix = null)
    {
        if (result == null || modelState == null || result.Errors == null)
            return;

        foreach (var kvp in result.Errors)
        {
            var key = string.IsNullOrWhiteSpace(modelPrefix) ? kvp.Key : $"{modelPrefix}.{kvp.Key}";
            foreach (var error in kvp.Value)
            {
                modelState.AddModelError(key, error);
            }
        }

        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            modelState.AddModelError(string.Empty, result.ErrorMessage);
        }
    }
}