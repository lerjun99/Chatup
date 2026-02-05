using ChatUp.Auth;
using ChatUp.BasicAuthenticationHandler;
using ChatUp.JwtAuth;
using DBContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using Tools;

IConfiguration config = new ConfigurationBuilder()
        .SetBasePath(Path.GetPathRoot(Environment.SystemDirectory))
        .AddJsonFile("app/chatup/appconfig.json", optional: true, reloadOnChange: true)
        .Build();

var builder = WebApplication.CreateBuilder(args);

// ------------------- Services -------------------

builder.Services.AddControllersWithViews();
// Add services to the container.
builder.Services.AddDbContext<ChatDBContext>(options =>
options.UseSqlServer((config["ConnectionStrings:Chatup_ConnectionString"])));
builder.Services.AddControllers()
      .AddJsonOptions(options =>
      {
          options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
          options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
      });
builder.Services.AddEndpointsApiExplorer();
builder.Configuration.AddJsonFile("appconfig.json", optional: true, reloadOnChange: true);


// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
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

    c.OperationFilter<FileUploadOperationFilter>(); // Optional for file uploads
});

// Authentication & Authorization
builder.Services.AddAuthentication("Basic")
    .AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>("Basic", null);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiKey", authBuilder =>
    {
        authBuilder.RequireRole("Administrators");
    });
});

// Singleton for JWT Authentication
builder.Services.AddSingleton<JwtAuthenticationManager>();

// ------------------- Build App -------------------
var app = builder.Build();

// ------------------- Middleware -------------------
app.UseHttpsRedirection();

// Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ChatUp API v1");
        c.RoutePrefix = string.Empty;
    });
}

// CORS (if needed for Blazor Server or WASM)
app.UseCors(builder =>
{
    builder.AllowAnyOrigin()
           .AllowAnyHeader()
           .AllowAnyMethod();
});

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Run the app
app.Run();
