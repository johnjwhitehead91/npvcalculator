using Npv.Core;
using Npv.Services;

namespace NpvCalculator.Services.Extensions;

using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all NPV application services
    /// </summary>
    public static IServiceCollection AddNpvServices(this IServiceCollection services)
    {
        // Register core services
        services.AddSingleton<INpvCalculatorService, NpvCalculatorService>();

        // Register additional services as needed in the future
        // services.AddScoped<IValidationService, ValidationService>();
        // services.AddScoped<ICacheService, CacheService>();

        return services;
    }

    /// <summary>
    /// Registers NPV services with configuration options
    /// </summary>
    public static IServiceCollection AddNpvServices(this IServiceCollection services,
        Action<NpvServiceOptions> configureOptions)
    {
        // Configure options
        services.Configure(configureOptions);

        // Register services
        return services.AddNpvServices();
    }
}

/// <summary>
/// Configuration options for NPV services
/// </summary>
public class NpvServiceOptions
{
    public int DefaultCalculationTimeoutMinutes { get; set; } = 30;
    public int MaxCashFlowPeriods { get; set; } = 100;
    public decimal MaxDiscountRate { get; set; } = 100.0m;
    public bool EnableCaching { get; set; } = true;
    public int CacheExpirationHours { get; set; } = 24;
}