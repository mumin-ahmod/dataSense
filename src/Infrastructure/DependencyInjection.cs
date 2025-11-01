using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using StackExchange.Redis;
using DataSenseAPI.Infrastructure.AppDb;
using DataSenseAPI.Infrastructure.Repositories;
using DataSenseAPI.Infrastructure.Services;
using DataSenseAPI.Application.Abstractions;
using DataSenseAPI.Domain.Models;

namespace DataSenseAPI.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer dependencies.
/// This includes repositories, external services, database, and identity configuration.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all Infrastructure layer services including repositories, external services, and persistence.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register Database Context
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Register Identity Services
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;

            // User settings
            options.User.RequireUniqueEmail = true;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Register Redis
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConnectionString));
        services.AddSingleton<IRedisService, RedisService>();

        // Register External Services
        services.AddHttpClient<OllamaService>();
        services.AddSingleton<IOllamaService, OllamaService>();
        services.AddSingleton<IKafkaService, KafkaService>();

        // Register Application Services (Infrastructure implementations)
        services.AddScoped<ISqlSafetyValidator, SqlSafetyValidator>();
        services.AddScoped<IBackendSqlGeneratorService, BackendSqlGeneratorService>();
        services.AddScoped<IBackendResultInterpreterService, BackendResultInterpreterService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<IQueryDetectionService, QueryDetectionService>();
        services.AddScoped<IAppMetadataService, AppMetadataService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IUserManagementService, UserManagementService>();

        // Register Email Services
        services.AddSingleton<IEmailQueue, EmailQueue>();
        services.AddScoped<IEmailSender, EmailSender>();

        // Register Background Services
        services.AddHostedService<KafkaOllamaConsumer>();
        services.AddHostedService<UserLockoutUnlockService>();
        services.AddHostedService<EmailSenderBackgroundService>();

        // Register Database Connection Factory
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

        // Register Repositories
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddScoped<IRequestLogRepository, RequestLogRepository>();
        services.AddScoped<IPricingRecordRepository, PricingRecordRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
        services.AddScoped<IUserSubscriptionRepository, UserSubscriptionRepository>();
        services.AddScoped<IUsageRequestRepository, UsageRequestRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IMessageChannelRepository, MessageChannelRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<IBillingEventRepository, BillingEventRepository>();
        services.AddScoped<IUsageRecordRepository, UsageRecordRepository>();

        return services;
    }
}

