using Microsoft.AspNetCore.Http;
using System;

namespace SearchAFile.Web.Helpers;

public static class HttpContextHelper
{
    // Marked volatile to ensure thread safety
    static volatile IServiceProvider services = null;

    /// <summary>
    /// Provides static access to the framework's services provider
    /// </summary>
    public static IServiceProvider Services
    {
        get
        {
            try
            {
                return services;
            }
            catch (Exception ex)
            {
                throw; // Preserve the original stack trace
            }
        }
        set
        {
            try
            {
                if (services != null)
                {
                    throw new Exception("Can't set once a value has already been set.");
                }
                services = value;
            }
            catch
            {
                throw; // Preserve the original stack trace
            }
        }
    }

    /// <summary>
    /// Provides static access to the current HttpContext
    /// </summary>
    public static HttpContext Current
    {
        get
        {
            try
            {
                if (services == null)
                {
                    throw new InvalidOperationException("Services provider is not set.");
                }

                IHttpContextAccessor httpContextAccessor = services.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;

                if (httpContextAccessor == null)
                {
                    throw new InvalidOperationException("IHttpContextAccessor service is not registered.");
                }

                return httpContextAccessor.HttpContext;
            }
            catch
            {
                throw; // Preserve the original stack trace
            }
        }
    }
}