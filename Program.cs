using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuanLyDatHang.Data;
using QuanLyDatHang.Hubs;
using QuanLyDatHang.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

// Configure DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("WebApiContext")));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
    
    // Configure SignalR to use JWT
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/chatHub") ||
                 path.StartsWithSegments("/notificationHub") ||
                 path.StartsWithSegments("/orderHub")))
            {
                context.Token = accessToken;
            }
            // Thêm hỗ trợ lấy token từ header Authorization
            else
            {
                var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader?.StartsWith("Bearer ") == true)
                {
                    context.Token = authHeader.Substring("Bearer ".Length).Trim();
                }
            }
            return Task.CompletedTask;
        }
    };
});

// Register services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<IVnPayService, VnPayService>(); // Sửa lại để ánh xạ IVnPayService với VnPayService
builder.Services.AddScoped<LocationService>();
builder.Services.AddScoped<ICommissionService, CommissionService>();
builder.Services.AddScoped<IRevenueStatisticsService, RevenueStatisticsService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Add HttpClient for LocationService
builder.Services.AddHttpClient<LocationService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapHub<ChatHub>("/chatHub");
app.MapHub<NotificationHub>("/notificationHub");
app.MapHub<OrderHub>("/orderHub");

app.Run();