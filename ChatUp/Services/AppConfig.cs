using Microsoft.JSInterop;
using Microsoft.Extensions.Configuration;

public static class AppConfig
{
    private static readonly IConfigurationRoot config;

    static AppConfig()
    {
        config = new ConfigurationBuilder()
             .SetBasePath(@"C:\app\chatup") // 👈 absolute path to your config
             .AddJsonFile("appconfig.json", optional: false, reloadOnChange: true)
             .Build();
    }

    // 🔗 Fetch Chat Urls
    public static string ChatUrl => config["Chat:Urls"] ?? "http://localhost:9001/";

    // 🔗 Fetch Connection String
    public static string ConnectionString => config.GetConnectionString("Chatup_ConnectionString");

    // 🔗 Fetch CORS origins
    public static IEnumerable<string> CorsOrigins =>
        config.GetSection("CORS:Origins").Get<IEnumerable<string>>() ?? Enumerable.Empty<string>();

    // 🔗 Fetch security header settings (sites allowed to embed the portal; CSP report-only switch)
    public static IEnumerable<string> FrameAncestors =>
        config.GetSection("SecurityHeaders:FrameAncestors").Get<IEnumerable<string>>() ?? Enumerable.Empty<string>();
    public static bool CspReportOnly => config.GetValue("SecurityHeaders:CspReportOnly", false);

    // 🔗 Fetch JWT Settings
    public static string JwtKey => config["Jwt:Key"] ?? "";
    public static string JwtIssuer => config["Jwt:Issuer"] ?? "";
}