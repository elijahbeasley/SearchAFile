using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net.Http;
using System.Text.Json;

namespace SearchAFile.Web.Helpers;

public static class ApiErrorHelper
{
    private class GeneralErrorResponse
    {
        public string? Message { get; set; }
        public string? Detail { get; set; }
    }

    public static Task AddErrorsToModelStateAsync<T>(ApiResult<T> result, ModelStateDictionary modelState, string? modelPrefix = null)
    {
        if (result == null || modelState == null || result.IsSuccess)
            return Task.CompletedTask;

        // Add model-specific validation errors
        if (result.Errors != null)
        {
            foreach (var kvp in result.Errors)
            {
                foreach (var message in kvp.Value)
                {
                    var key = string.IsNullOrWhiteSpace(modelPrefix)
                        ? kvp.Key
                        : $"{modelPrefix}.{kvp.Key}";

                    // Avoid duplicates
                    if (!modelState.ContainsKey(key) || !modelState[key].Errors.Any(e => e.ErrorMessage == message))
                    {
                        modelState.AddModelError(key, message);
                    }
                }
            }
        }

        // Add general message fallback
        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            modelState.AddModelError(string.Empty, result.ErrorMessage);
        }

        return Task.CompletedTask;
    }
    public class ApiErrorResponse
    {
        public Dictionary<string, string[]> Errors { get; set; } = new();
    }

    public static async Task<string> ExtractApiErrorAsync(HttpResponseMessage response)
    {
        if (response == null)
            return "An unknown error occurred.";

        var content = await response.Content.ReadAsStringAsync();

        try
        {
            var generalError = JsonSerializer.Deserialize<GeneralErrorResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (!string.IsNullOrWhiteSpace(generalError?.Message))
                return generalError.Message;

            if (!string.IsNullOrWhiteSpace(generalError?.Detail))
                return generalError.Detail;

            return "An unexpected error occurred.";
        }
        catch
        {
            return "Failed to parse API error response.";
        }
    }
}