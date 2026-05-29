using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JobSearch.Application.Services;

public static class JobImportServiceCollectionExtensions
{
    public const string EnabledConfigurationKey = "JobImport:Enabled";
    public const string ProviderConfigurationKey = "JobImport:Provider";
    public const string RemotiveProviderName = "Remotive";
    public const string RemotiveBaseUrlConfigurationKey = "JobImport:RemotiveBaseUrl";
    public const string ConfigurationSectionName = "JobImport";
    public const string DefaultRemotiveBaseUrl = "https://remotive.com";

    public static IServiceCollection AddConfiguredJobImport(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration[ProviderConfigurationKey];
        if (!RemotiveProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("JobImport:Provider must be Remotive.");
        }

        var baseUrl = configuration[RemotiveBaseUrlConfigurationKey] ?? DefaultRemotiveBaseUrl;
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException("JobImport:RemotiveBaseUrl must be an absolute URL.");
        }

        services.AddSingleton<IValidateOptions<RemotiveJobImportOptions>, RemotiveJobImportOptionsValidator>();
        services.AddOptions<RemotiveJobImportOptions>()
            .Bind(configuration.GetSection(ConfigurationSectionName))
            .ValidateOnStart();

        services.AddHttpClient<IJobImportService, RemotiveJobImportService>(client =>
        {
            client.BaseAddress = baseUri;
        });

        return services;
    }

    public static bool IsEnabled(IConfiguration configuration) =>
        bool.TryParse(configuration[EnabledConfigurationKey], out var enabled) && enabled;
}
