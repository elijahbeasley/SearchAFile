using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net.Http;
using System.Text.Json;

namespace SearchAFile.Web.Helpers;

public static class ApiErrorHelper
{
    public static async Task AddErrorsToModelStateAsync(HttpResponseMessage response, ModelStateDictionary modelState, string? modelPrefix = null)
    {
        if (response == null || modelState == null) return;

        var content = await response.Content.ReadAsStringAsync();

        var errorObj = JsonSerializer.Deserialize<ApiErrorResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (errorObj?.Errors == null) return;

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
    }

    private class ApiErrorResponse
    {
        public Dictionary<string, string[]> Errors { get; set; } = new();
    }
}
