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

    public static async Task AddErrorsToModelStateAsync(HttpResponseMessage response, ModelStateDictionary modelState, string? modelPrefix = null)
    {
        if (response == null || modelState == null) return;

        var content = await response.Content.ReadAsStringAsync();

        var errorObj = JsonSerializer.Deserialize<ApiErrorResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Handle ModelState errors
        if (errorObj?.Errors != null && errorObj.Errors.Count > 0)
        {
            foreach (var kvp in errorObj.Errors)
            {
                foreach (var message in kvp.Value)
                {
                    var key = string.IsNullOrWhiteSpace(modelPrefix)
                        ? kvp.Key
                        : $"{modelPrefix}.{kvp.Key}";

                    modelState.AddModelError(key, message);
                }
            }

            return;
        }

        // Handle general error messages
        var generalError = JsonSerializer.Deserialize<GeneralErrorResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (!string.IsNullOrWhiteSpace(generalError?.Message))
        {
            modelState.AddModelError(string.Empty, generalError.Message);
        }

        if (!string.IsNullOrWhiteSpace(generalError?.Detail))
        {
            modelState.AddModelError(string.Empty, generalError.Detail);
        }
    }

    private class ApiErrorResponse
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