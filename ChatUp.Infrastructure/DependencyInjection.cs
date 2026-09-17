using ChatUp.Application.Common.Interfaces;
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

            services.AddControllers()
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
                    "Bearer",
                    new OpenApiSecurityScheme
                    {
                        Description =
                            "JWT Authorization header using the Bearer scheme " +
                            "(Example: 'Bearer 12345abcdef')",

                        Name = "Authorization",

                        In = ParameterLocation.Header,

                        Type = SecuritySchemeType.ApiKey,

                        Scheme = "Bearer"
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

                                        Id = "Bearer"
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

            services.AddAuthentication("Basic")
                .AddScheme<
                    BasicAuthenticationOptions,
                    BasicAuthenticationHandler>(
                        "Basic",
                        null);


            // ============================================================
            // AUTHORIZATION
            // ============================================================

            services.AddAuthorization(options =>
            {
                options.AddPolicy(
                    "ApiKey",
                    authBuilder =>
                    {
                        authBuilder.RequireRole(
                            "Administrators");
                    });
            });


            // ============================================================
            // RETURN
            // ============================================================

            return services;
        }
    }
}