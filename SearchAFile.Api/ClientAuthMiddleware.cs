using Microsoft.Extensions.Configuration;

public class ClientAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public ClientAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = context.Request.Headers["X-Client-Id"].FirstOrDefault();
        var clientSecret = context.Request.Headers["X-Client-Secret"].FirstOrDefault();

        var configClientId = _configuration["ApiAuth:ClientId"];
        var configClientSecret = _configuration["ApiAuth:ClientSecret"];

        //if (clientId != configClientId || clientSecret != configClientSecret)
        //{
        //    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        //    await context.Response.WriteAsync("Unauthorized: Invalid client credentials.");
        //    return;
        //}

        await _next(context);
    }
}