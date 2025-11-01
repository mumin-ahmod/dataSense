using Microsoft.Extensions.DependencyInjection;
using MediatR;

namespace DataSenseAPI.Application;

/// <summary>
/// Extension methods for registering Application layer dependencies.
/// This follows Clean Architecture principles by keeping application services separate.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Application layer services including MediatR handlers.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register MediatR and automatically discover handlers from the Application assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(Commands.GenerateSql.GenerateSqlCommand).Assembly));

        return services;
    }
}

