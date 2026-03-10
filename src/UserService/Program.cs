using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UserService.Data;
using UserService.Repositories;
using UserService.Services;

LoadEnvironmentFromDotEnv(".env", Path.Combine("src", "UserService", ".env"));

var builder = WebApplication.CreateBuilder(args);
string? GetConfig(string key) => builder.Configuration[key.Replace("__", ":")] ?? builder.Configuration[key];

builder.Services.AddControllers();

var userDbConnection = GetConfig("ConnectionStrings__UserDb");
if (string.IsNullOrWhiteSpace(userDbConnection))
{
    throw new InvalidOperationException("Missing configuration value: ConnectionStrings__UserDb");
}

builder.Services.AddDbContext<UserDbContext>(options => options.UseNpgsql(userDbConnection));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService.Services.UserService>();
builder.Services.AddScoped<ICognitoService, CognitoService>();

var cognitoUserPoolId = GetConfig("Cognito__UserPoolId");
var cognitoClientId = GetConfig("Cognito__ClientId");
var cognitoRegion = GetConfig("Cognito__Region");

if (string.IsNullOrWhiteSpace(cognitoUserPoolId) || string.IsNullOrWhiteSpace(cognitoClientId) || string.IsNullOrWhiteSpace(cognitoRegion))
{
    throw new InvalidOperationException("Missing Cognito configuration. Set Cognito__UserPoolId, Cognito__ClientId, and Cognito__Region.");
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
            // Cognito access tokens use `client_id` instead of a standard `aud` claim.
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    dbContext.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
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
