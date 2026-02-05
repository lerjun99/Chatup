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

namespace ChatUp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ------------------- EF Core -------------------
        services.AddDbContext<ChatDBContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Chatup_ConnectionString")));

        services.AddScoped<IChatDBContext>(sp => sp.GetRequiredService<ChatDBContext>());
        services.AddScoped<IJwtTokenService, JwtAuthenticationManager>();
        services.AddScoped<ICryptography, Cryptography>();
        // ------------------- Repository Registrations -------------------
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddHttpContextAccessor();
        // ------------------- Handlers (Application Layer) -------------------
        services.AddScoped<GetConversationHandler>();
        services.AddScoped<SendMessageCommandHandler>();
        services.AddScoped<IChatHubContext, ChatHubContext>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IEmailOtpRepository, EmailOtpRepository>();
        services.AddScoped<IUserStatusNotifier, UserStatusNotifier>();
        services.AddScoped<IClientContext, ClientContext>();
        services.AddHttpClient<IPublicIpService, PublicIpService>();
        // Register MediatR (scans the Application assembly for handlers)
        // ------------------- Services (Infrastructure Layer) -------------------
        services.AddScoped<NotificationService>();
        services.AddSignalR();
        // ------------------- Controllers + JSON Options -------------------
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        // ------------------- Swagger -------------------
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "ChatUp API",
                Description = "Chat API for Blazor Live Chat"
            });

            // JWT Security Definition
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme (Example: 'Bearer 12345abcdef')",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });

            c.OperationFilter<FileUploadOperationFilter>();
        });

        // ------------------- Authentication -------------------
        services.AddAuthentication("Basic")
            .AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>("Basic", null);

        // ------------------- Authorization -------------------
        services.AddAuthorization(options =>
        {
            options.AddPolicy("ApiKey", authBuilder =>
            {
                authBuilder.RequireRole("Administrators");
            });
        });

        return services;
    }
}
