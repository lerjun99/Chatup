using ChatUp.Application.Common.Behaviors;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using FluentValidation;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Reflection;
using System.Text.Json.Serialization;
namespace ChatUp.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Register MediatR (handlers, commands, queries)
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

            // Register all validators
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // Register pipeline behaviors
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            // Audit log: one row per command with its exact data changes (see Auditing/ActivityLoggingBehavior)
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Auditing.ActivityLoggingBehavior<,>));

            // Chat auto replies are sent in the background from their own DI scope
            services.AddSingleton<Features.AutoReply.ITicketAutoReplyService, Features.AutoReply.TicketAutoReplyService>();

            // Bell notifications are saved and pushed in the background from their own DI scope
            services.AddSingleton<Features.Notifications.INotificationPublisher, Features.Notifications.NotificationPublisher>();
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
            // Setup Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/chatup-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10)
                .CreateLogger();

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(dispose: true);
            });

            return services;
        }
    }
}
