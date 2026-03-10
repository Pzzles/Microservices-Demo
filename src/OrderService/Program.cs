using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderService.Data;
using OrderService.Repositories;
using OrderService.Services;

LoadEnvironmentFromDotEnv(".env", Path.Combine("src", "OrderService", ".env"));

var builder = WebApplication.CreateBuilder(args);
string? GetConfig(string key) => builder.Configuration[key.Replace("__", ":")] ?? builder.Configuration[key];

builder.Services.AddControllers();

var connectionString = GetConfig("ConnectionStrings__OrderDb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Missing configuration value: ConnectionStrings__OrderDb");
}

builder.Services.AddDbContext<OrderDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService.Services.OrderService>();
builder.Services.AddScoped<IProductValidationService, ProductValidationService>();

var productServiceUrl = GetConfig("ServiceUrls__ProductService");
if (string.IsNullOrWhiteSpace(productServiceUrl))
{
    throw new InvalidOperationException("Missing configuration value: ServiceUrls__ProductService");
}

builder.Services.AddHttpClient("ProductService", client =>
{
    client.BaseAddress = new Uri(productServiceUrl);
});

var cognitoRegion = GetConfig("Cognito__Region");
var cognitoUserPoolId = GetConfig("Cognito__UserPoolId");
if (string.IsNullOrWhiteSpace(cognitoRegion) || string.IsNullOrWhiteSpace(cognitoUserPoolId))
{
    throw new InvalidOperationException("Missing Cognito configuration. Set Cognito__Region and Cognito__UserPoolId.");
}

var authority = $"https://cognito-idp.{cognitoRegion}.amazonaws.com/{cognitoUserPoolId}";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authority,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    dbContext.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static void LoadEnvironmentFromDotEnv(params string[] candidates)
{
    foreach (var candidate in candidates)
    {
        var path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), candidate));
        if (!File.Exists(path))
        {
            continue;
        }

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#"))
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
            {
                line = line[7..].Trim();
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }

        return;
    }
}
