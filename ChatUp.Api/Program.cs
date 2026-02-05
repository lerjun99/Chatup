using ChatUp.Application;
using ChatUp.Application.Common.Interfaces;
using ChatUp.Application.Common.Mappings;
using ChatUp.Application.Features.Client.Commands;
using ChatUp.Application.Features.Contracts.Commands;
using ChatUp.Application.Features.EmailOTP.Commands;
using ChatUp.Application.Features.TicketMessage.Commands;
using ChatUp.Application.Features.TicketMessage.Handlers;
using ChatUp.Application.Features.User.Queries;
using ChatUp.Application.Features.UserApplicant.Handlers;
using ChatUp.Domain.Interfaces;
using ChatUp.Infrastructure;
using ChatUp.Infrastructure.Hubs;
using ChatUp.Infrastructure.Persistence.Repositories;
using ChatUp.Infrastructure.Services;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = new ConfigurationBuilder()
        .SetBasePath(Path.GetPathRoot(Environment.SystemDirectory))
        .AddJsonFile("app/chatup/appconfig.json", optional: true, reloadOnChange: true)
        .Build();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(GetUsersQuery).Assembly));
// Add services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(configuration);
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(ChatUp.Application.Features.Client.Queries.GetClientByIdQuery).Assembly));
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(GetUserAssignedClientsCommand).Assembly));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    typeof(CreateTicketHandler).Assembly,
    typeof(GetTicketByIdHandler).Assembly,
    typeof(UpdateApplicantHandler).Assembly,
    typeof(UploadTicketFileHandler).Assembly,
    typeof(CreateTicketCommand).Assembly,
    typeof(ChangeLogPasswordCommand).Assembly,
    typeof(SendScheduledEmailsCommand).Assembly
));
builder.Services.AddHostedService<EmailSchedulerService>();
builder.Services.AddSignalR();
builder.Services.AddHostedService<SlaBackgroundService>();
// Register AutoMapper and scan for profiles in this assembly
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddMaps(typeof(MappingProfile).Assembly);
});
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IUserClientRepository, UserClientRepository>();
builder.Services.AddScoped<IEmailOtpRepository, EmailOtpRepository>();
builder.Services.AddScoped<ContractService>();
builder.Services.AddScoped<ILoginHistoryRepository, LoginHistoryRepository>();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddScoped<IChatHubContext, ChatHubContext>();
//builder.Services.AddHostedService<ContractExpiryNotificationService>();
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ChatUp.Application.Common.Behaviors.ValidationBehavior<,>));
var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                       ForwardedHeaders.XForwardedProto,

    // Example (adjust to your infra)
    KnownNetworks = { },
    KnownProxies = { }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ChatUp API v1");
        c.DocExpansion(DocExpansion.None);
    });
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads")),
    RequestPath = "/uploads"
});
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ✅ Map hub
app.MapHub<UserStatusHub>("/userstatushub");
app.MapHub<SlaHub>("/slahub");
app.MapHub<ChatHub>("/chathub");

app.Run();