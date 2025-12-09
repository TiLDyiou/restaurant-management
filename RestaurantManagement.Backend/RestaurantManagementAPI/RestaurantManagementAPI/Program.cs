using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RestaurentManagementAPI.Data;
using RestaurentManagementAPI.Hubs;
using RestaurentManagementAPI.Seeders;
using RestaurentManagementAPI.Services;
using System.Runtime;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// DbContext
builder.Services.AddDbContext<QLNHDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("QLNHDatabase")));


// Controllers & SignalR
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();


// Swagger + JWT Auth
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token của bạn vào ô bên dưới.\n\nVí dụ: \"Bearer 12345abcdef\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// JWT Authentication
var jwt = configuration.GetSection("Jwt");
var key = jwt.GetValue<string>("Key") ?? "default_secret_key_12345"; // tránh null
var issuer = jwt.GetValue<string>("Issuer");
var audience = jwt.GetValue<string>("Audience");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = !string.IsNullOrEmpty(issuer),
        ValidateAudience = !string.IsNullOrEmpty(audience),
        ValidIssuer = issuer,
        ValidAudience = audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<RestaurentManagementAPI.Services.IOrderService, RestaurentManagementAPI.Services.OrderService>();
var app = builder.Build();

// Seed dữ liệu admin và bàn
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
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Đã xảy ra lỗi khi seeding admin.");
}

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapHub<KitchenHub>("/kitchenHub");
app.UseAuthentication();
app.UseAuthorization();

// Map Controllers & SignalR Hubs
app.MapControllers();
app.MapHub<BanHub>("/banHub"); // Hub cho trạng thái bàn realtime
app.Run();
