using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderService.Data;
using OrderService.Repositories;
using OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString = builder.Configuration["ConnectionStrings__OrderDb"];
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Missing configuration value: ConnectionStrings__OrderDb");
}

builder.Services.AddDbContext<OrderDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService.Services.OrderService>();
builder.Services.AddScoped<IProductValidationService, ProductValidationService>();

var productServiceUrl = builder.Configuration["ServiceUrls__ProductService"];
if (string.IsNullOrWhiteSpace(productServiceUrl))
{
    throw new InvalidOperationException("Missing configuration value: ServiceUrls__ProductService");
}

builder.Services.AddHttpClient("ProductService", client =>
{
    client.BaseAddress = new Uri(productServiceUrl);
});

var cognitoRegion = builder.Configuration["Cognito__Region"];
var cognitoUserPoolId = builder.Configuration["Cognito__UserPoolId"];
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
