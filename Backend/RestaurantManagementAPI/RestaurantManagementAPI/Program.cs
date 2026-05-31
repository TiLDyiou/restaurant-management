using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RestaurantManagementAPI.Data;
using RestaurantManagementAPI.Infrastructure.Email;
using RestaurantManagementAPI.Infrastructure.Security;
using RestaurantManagementAPI.Infrastructure.Sockets;
using RestaurantManagementAPI.Interfaces;
using RestaurantManagementAPI.Middleware;
using RestaurantManagementAPI.Seeders;
using RestaurantManagementAPI.Services;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Set up Serilog
builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day));

// DbContext
builder.Services.AddDbContext<QLNHDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("QLNHDatabase")));

// Health Checks
builder.Services.AddHealthChecks()
    .AddSqlServer(configuration.GetConnectionString("QLNHDatabase") ?? throw new InvalidOperationException("Connection string 'QLNHDatabase' not found."));

// Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token: Bearer {token}"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Production",
        builder =>
        {
            builder.WithOrigins(
                    "https://qlnhnhom2.me",
                    "https://localhost:7004",
                    "http://localhost:5276"
                )
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

// JWT Auth Configuration
var jwt = configuration.GetSection("Jwt");
var key = jwt.GetValue<string>("Key")
    ?? throw new InvalidOperationException("JWT Key is not configured. Set Jwt:Key in user-secrets or environment variables.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/restaurantChatHub") || path.StartsWithSegments("/restaurantHub")))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("otp", opt =>
    {
        opt.PermitLimit = 3;
        opt.Window = TimeSpan.FromMinutes(5);
        opt.QueueLimit = 0;
    });

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// Dependency Injection (Đăng ký Services)

// Infrastructure
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddSingleton<IRealtimeNotifier, SignalRNotifier>();
builder.Services.AddHostedService<TcpSocketServer>();
builder.Services.AddSignalR();

// Business Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IDishService, DishService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IChatService, ChatService>();


var app = builder.Build();
try
{
    using (var scope = app.Services.CreateScope())
    {
        await DataSeeder.SeedAdminAsync(scope.ServiceProvider);
        await BanSeeder.SeedTableAsync(scope.ServiceProvider);
    }
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Seeding Error");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseStaticFiles();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseRouting();
app.UseCors("Production");
app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<RestaurantChatHub>("/restaurantChatHub");
app.MapHub<RestaurantHub>("/restaurantHub");
app.MapHealthChecks("/health");
app.Run();