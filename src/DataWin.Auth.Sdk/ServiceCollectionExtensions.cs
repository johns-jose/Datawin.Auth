using Microsoft.Extensions.DependencyInjection;

namespace DataWin.Auth.Sdk;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataWinAuthSdk(
        this IServiceCollection services,
        string baseUrl,
        string? signingKey = null,
        string? issuer = null)
    {
        services.AddHttpClient<DataWinAuthClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        return services;
    }
}
