using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Common.Security;
using ChatUp.Application.Features.Messages.Handlers;
using ChatUp.Domain.Interfaces;
using ChatUp.Infrastructure.Common;
using ChatUp.Infrastructure.Persistence;
using ChatUp.Infrastructure.Persistence.Repositories;
using ChatUp.Infrastructure.Services;
using ChatUp.Infrastructure.Services.Auth;
using ChatUp.Infrastructure.Services.BasicAuthenticationHandler;
using ChatUp.Infrastructure.Services.Cryptography;
using ChatUp.Infrastructure.Services.FileUploadOperationFilter;
using ChatUp.Infrastructure.Services.JwtAuth;
using ChatUp.Infrastructure.Services.Security;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text.Json.Serialization;

namespace ChatUp.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // ============================================================
            // EF CORE
            // ============================================================

            services.AddDbContext<ChatDBContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString(
                        "Chatup_ConnectionString")));

            services.AddScoped<IChatDBContext>(
                sp => sp.GetRequiredService<ChatDBContext>());


            // ============================================================
            // SUPPORT NOTIFICATION CONFIGURATION
            // ============================================================

            var supportNotificationSection =
                configuration.GetSection("SupportNotification");

            if (!supportNotificationSection.Exists())
            {
                throw new InvalidOperationException(
                    "The 'SupportNotification' section was not found in appsettings.json.");
            }

            var supportNotificationSettings =
                supportNotificationSection
                    .Get<SupportNotificationSettings>();

            if (supportNotificationSettings == null)
            {
                throw new InvalidOperationException(
                    "SupportNotification configuration could not be loaded.");
            }

            if (string.IsNullOrWhiteSpace(
                supportNotificationSettings.Email))
            {
                throw new InvalidOperationException(
                    "SupportNotification:Email is not configured.");
            }

            if (supportNotificationSettings.ReminderMinutes <= 0)
            {
                throw new InvalidOperationException(
                    "SupportNotification:ReminderMinutes must be greater than 0.");
            }

            Console.WriteLine(
                "================================================");

            Console.WriteLine(
                "Support Notification Configuration");

            Console.WriteLine(
                $"Support Email: {supportNotificationSettings.Email}");

            Console.WriteLine(
                $"Reminder Minutes: {supportNotificationSettings.ReminderMinutes}");

            Console.WriteLine(
                "================================================");

            /*
             * IMPORTANT:
             *
             * Register the actual populated settings object.
             *
             * SupportResponseReminderService receives:
             *
             * SupportNotificationSettings settings
             *
             * NOT:
             *
             * IOptions<SupportNotificationSettings>
             */
            services.AddSingleton(
                supportNotificationSettings);


            // ============================================================
            // AUTHENTICATION / SECURITY
            // ============================================================

            services.AddScoped<
                IJwtTokenService,
                JwtAuthenticationManager>();

            services.AddScoped<
                ICryptography,
                Cryptography>();


            // ============================================================
            // REPOSITORIES
            // ============================================================

            services.AddScoped<
                IMessageRepository,
                MessageRepository>();

            services.AddHttpContextAccessor();


            // ============================================================
            // HANDLERS
            // ============================================================

            services.AddScoped<
                GetConversationHandler>();

            services.AddScoped<
                SendMessageCommandHandler>();

            services.AddScoped<
                IChatHubContext,
                ChatHubContext>();


            // ============================================================
            // TICKET / USER / PROJECT
            // ============================================================

            services.AddScoped<
                ITicketRepository,
                TicketRepository>();

            services.AddScoped<
                IUserRepository,
                UserRepository>();

            services.AddScoped<
                IProjectRepository,
                ProjectRepository>();

            services.AddScoped<
                IEmailOtpRepository,
                EmailOtpRepository>();


            // ============================================================
            // OTHER SERVICES
            // ============================================================

            services.AddScoped<
                IUserStatusNotifier,
                UserStatusNotifier>();

            services.AddScoped<
                IBusinessCalendarService,
                BusinessCalendarService>();

            services.AddScoped<
                IClientContext,
                ClientContext>();


            // ============================================================
            // HTTP CLIENTS
            // ============================================================

            services.AddHttpClient<
                IPublicIpService,
                PublicIpService>();

            services.AddHttpClient<
                IHolidayApiService,
                HolidayApiService>();


            // ============================================================
            // BACKGROUND SERVICES
            // ============================================================

            services.AddHostedService<
                SupportResponseReminderService>();

            services.AddScoped<
                IBusinessCalendarRepository,
                BusinessCalendarRepository>();


            // ============================================================
            // NOTIFICATION SERVICE
            // ============================================================

            services.AddScoped<
                NotificationService>();


            // ============================================================
            // SLA STATUS CACHE
            // ============================================================

            services.AddSingleton<
                SlaStatusCache>();

            services.AddHostedService<
                SlaBackgroundService>();


            // ============================================================
            // CONTROLLERS
            // ============================================================

            // Every controller requires a valid API key unless it opts out with [AllowAnonymous]
            services.AddControllers(options =>
                    options.Filters.Add(new AuthorizeFilter(ApiKeyDefaults.ApiAccessPolicy)))
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler =
                        ReferenceHandler.IgnoreCycles;

                    options.JsonSerializerOptions.DefaultIgnoreCondition =
                        JsonIgnoreCondition.WhenWritingNull;
                });


            // ============================================================
            // SWAGGER
            // ============================================================

            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(
                    "v1",
                    new OpenApiInfo
                    {
                        Version = "v1",
                        Title = "ChatUp API",
                        Description =
                            "Chat API for Blazor Live Chat"
                    });

                c.AddSecurityDefinition(
                    ApiKeyDefaults.Scheme,
                    new OpenApiSecurityScheme
                    {
                        Description =
                            "API key for the ChatUp API. Send it as 'X-Api-Key: <key>' " +
                            "(or 'Authorization: Bearer <key>'). Manage keys in the UI under Settings → API keys.",

                        Name = ApiKeyDefaults.HeaderName,

                        In = ParameterLocation.Header,

                        Type = SecuritySchemeType.ApiKey
                    });

                c.AddSecurityRequirement(
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference =
                                    new OpenApiReference
                                    {
                                        Type =
                                            ReferenceType.SecurityScheme,

                                        Id = ApiKeyDefaults.Scheme
                                    }
                            },

                            Array.Empty<string>()
                        }
                    });

                c.OperationFilter<
                    FileUploadOperationFilter>();
            });


            // ============================================================
            // AUTHENTICATION
            // ============================================================

            // API keys: hashed lookup, cached validation (see ApiKeyAuthenticationHandler)
            services.AddMemoryCache();
            services.AddScoped<ApiKeyService>();

            services.AddAuthentication(ApiKeyDefaults.Scheme)
                .AddScheme<
                    ApiKeyAuthenticationOptions,
                    ApiKeyAuthenticationHandler>(
                        ApiKeyDefaults.Scheme,
                        null);


            // ============================================================
            // AUTHORIZATION
            // ============================================================

            services.AddAuthorization(options =>
            {
                // Any valid API key — applied globally to controllers and hubs
                options.AddPolicy(
                    ApiKeyDefaults.ApiAccessPolicy,
                    policy => policy
                        .AddAuthenticationSchemes(ApiKeyDefaults.Scheme)
                        .RequireAuthenticatedUser());

                // API key management: an Admin key, or any key used by a signed-in administrator
                options.AddPolicy(
                    ApiKeyDefaults.AdminPolicy,
                    policy => policy
                        .AddAuthenticationSchemes(ApiKeyDefaults.Scheme)
                        .RequireAuthenticatedUser()
                        .RequireAssertion(ctx =>
                            ctx.User.IsInRole(nameof(ChatUp.Domain.Entities.ApiKeyRole.Admin)) ||
                            ctx.User.HasClaim(ApiKeyDefaults.UserTypeClaim, "1")));

                options.DefaultPolicy = options.GetPolicy(ApiKeyDefaults.ApiAccessPolicy)!;
            });


            // ============================================================
            // RETURN
            // ============================================================

            return services;
        }
    }
}