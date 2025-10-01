using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SearchAFile.Core.Interfaces;
using SearchAFile.Core.Options;
using SearchAFile.Infrastructure.Services;

namespace SearchAFile.Infrastructure.DependencyInjection;

/// <summary>
/// One-liner to wire everything up.
/// </summary>
public static class OpenAIServiceCollectionExtensions
{
    public static IServiceCollection AddOpenAIAndStorage(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<OpenAIOptions>(config.GetSection(OpenAIOptions.SectionName));
        services.Configure<PhysicalStorageOptions>(config.GetSection(PhysicalStorageOptions.SectionName));

        services.AddHttpClient<IOpenAIVectorStoreService, OpenAIVectorStoreService>();
        services.AddHttpClient<IOpenAIFileService, OpenAIFileService>();

        // Physical file service can stay singleton
        services.AddSingleton<IPhysicalFileService, PhysicalFileService>();

        return services;
    }
}