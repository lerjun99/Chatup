using Blazored.LocalStorage;
using Blazored.SessionStorage;
using ChatUp.ChatHub;
using ChatUp.Services;
using ChatUp.SlaHub;
using ChatUp.UserStatusHub;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Fast.Components.FluentUI;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddHttpClient();
builder.Services.AddDataProtection();
builder.Services.AddScoped<LinkService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<TitleService>();
builder.Services.AddScoped<ContractService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<UserClientAssignmentService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ProjectAssignmentService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<TicketNotificationService>();
builder.Services.AddHostedService<ContractExpiryBackgroundService>();
builder.Services.AddScoped<PasswordRecoveryService>();
builder.Services.AddScoped<ApplicantApiService>();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredSessionStorage();
// #3 ‑ Your own DI registrations -----------------
builder.Services.AddScoped<UserState>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddServerSideBlazor()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB
    });
builder.Services.AddFluentUIComponents();
builder.Services.AddBlazoredSessionStorage();
var app = builder.Build();
app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.MapBlazorHub();
app.MapHub<ChatHub>("/chathub");
app.MapHub<UserStatusHub>("userstatushub");
app.MapHub<SlaHub>("/slahub");
app.MapFallbackToPage("/_Host");

app.Run();
