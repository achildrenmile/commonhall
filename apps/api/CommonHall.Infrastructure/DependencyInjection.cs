using System.Threading.Channels;
using CommonHall.Application.Interfaces;
using CommonHall.Domain.Entities;
using CommonHall.Infrastructure.BackgroundServices;
using CommonHall.Infrastructure.Persistence;
using CommonHall.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CommonHall.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<CommonHallDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(CommonHallDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<CommonHallDbContext>());

        // Identity
        services.AddIdentity<User, IdentityRole<Guid>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<CommonHallDbContext>()
            .AddDefaultTokenProviders();

        // Redis
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(redisConnection));
            services.AddScoped<ICacheService, RedisCacheService>();
        }

        // Seeder
        services.AddScoped<DbSeeder>();

        // Auth Service
        services.AddScoped<IAuthService, AuthService>();

        // Slug Service
        services.AddScoped<ISlugService, SlugService>();

        // Content Authorization Service
        services.AddScoped<IContentAuthorizationService, ContentAuthorizationService>();

        // Tag Service
        services.AddScoped<ITagService, TagService>();

        // View Count Service
        services.AddScoped<IViewCountService, ViewCountService>();

        // File Storage Service
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        // File Validation Service
        services.AddScoped<IFileValidationService, FileValidationService>();

        // Targeting Service
        services.AddScoped<ITargetingService, TargetingService>();

        // Rule-Based Group Service
        services.AddScoped<IRuleBasedGroupService, RuleBasedGroupService>();

        // Email Services
        services.AddScoped<IEmailRenderer, EmailRenderer>();
        services.AddScoped<INewsletterService, NewsletterService>();

        // Email sending service - use SMTP for dev, SendGrid for prod
        services.Configure<SmtpSettings>(configuration.GetSection("Smtp"));
        services.Configure<SendGridSettings>(configuration.GetSection("SendGrid"));

        var useSendGrid = !string.IsNullOrEmpty(configuration["SendGrid:ApiKey"]);
        if (useSendGrid)
        {
            services.AddHttpClient<IEmailSendingService, SendGridEmailService>();
        }
        else
        {
            services.AddScoped<IEmailSendingService, SmtpEmailService>();
        }

        // Email sending queue
        services.AddSingleton(Channel.CreateUnbounded<NewsletterSendJob>(
            new UnboundedChannelOptions { SingleReader = true }));

        // Notification Service
        services.AddScoped<INotificationService, NotificationService>();

        // Journey Services
        services.AddScoped<IJourneyTriggerService, JourneyTriggerService>();
        services.AddScoped<IJourneyService, JourneyService>();

        // Background Services
        services.AddHostedService<ScheduledPublishingService>();
        services.AddHostedService<RuleBasedGroupRecalculationService>();
        services.AddHostedService<EmailSendingBackgroundService>();
        services.AddHostedService<ScheduledNewsletterService>();
        services.AddHostedService<JourneyProgressionService>();

        return services;
    }
}
