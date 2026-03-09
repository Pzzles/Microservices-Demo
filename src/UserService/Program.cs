using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UserService.Data;
using UserService.Repositories;
using UserService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var userDbConnection = builder.Configuration["ConnectionStrings__UserDb"];
if (string.IsNullOrWhiteSpace(userDbConnection))
{
    throw new InvalidOperationException("Missing configuration value: ConnectionStrings__UserDb");
}

builder.Services.AddDbContext<UserDbContext>(options => options.UseNpgsql(userDbConnection));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService.Services.UserService>();
builder.Services.AddScoped<ICognitoService, CognitoService>();

var cognitoUserPoolId = builder.Configuration["Cognito__UserPoolId"];
var cognitoClientId = builder.Configuration["Cognito__ClientId"];
var cognitoRegion = builder.Configuration["Cognito__Region"];

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
