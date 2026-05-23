// ============================================================
// DestinoPeru - Program.cs
// Configuración principal — compatible con .NET 8
// ============================================================
using System.Text;
using Npgsql;
using DestinoPeruAPI.Application.Interfaces;
using DestinoPeruAPI.Application.Services;
using DestinoPeruAPI.Infrastructure;
using DestinoPeruAPI.Infrastructure.Data;
using DestinoPeruAPI.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------
// 1. Base de Datos — PostgreSQL con Entity Framework Core
// -------------------------------------------------------
var connectionString = ResolveConnectionString(builder.Configuration);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

static string ResolveConnectionString(IConfiguration configuration)
{
    // Railway inyecta DATABASE_PRIVATE_URL (red interna) o DATABASE_URL
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_PRIVATE_URL")
        ?? Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrWhiteSpace(databaseUrl))
        return ParseRailwayDatabaseUrl(databaseUrl);

    var fromConfig = configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(fromConfig))
        return fromConfig;

    throw new InvalidOperationException(
        "No hay cadena de conexion. En Railway: vincula el servicio PostgreSQL a la API o define ConnectionStrings__DefaultConnection.");
}

static string ParseRailwayDatabaseUrl(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.TrimStart('/').Split('?')[0],
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
        SslMode = SslMode.Require
    };
    return builder.ConnectionString;
}

// -------------------------------------------------------
// 2. Inyección de Dependencias
// -------------------------------------------------------
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAgencyRepository, AgencyRepository>();
builder.Services.AddScoped<ITourRepository, TourRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TourService>();
builder.Services.AddScoped<AgencyService>();
builder.Services.AddScoped<ReservationService>();
builder.Services.AddScoped<PaymentService>();

builder.Services.AddScoped<IJwtService, JwtService>();

// -------------------------------------------------------
// 3. JWT Authentication
// -------------------------------------------------------
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey   = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtSettings["Issuer"],
            ValidAudience            = jwtSettings["Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// -------------------------------------------------------
// 4. CORS — permite llamadas desde Blazor
// -------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorPolicy", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
                origin.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase)
                || origin.StartsWith("https://localhost:", StringComparison.OrdinalIgnoreCase)
                || (origin.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    && origin.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase)))
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// -------------------------------------------------------
// 5. Controllers + Swagger (compatible .NET 8)
// -------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "DestinoPeru API",
        Version     = "v1",
        Description = "Marketplace de turismo peruano"
    });

    // Botón Authorize en Swagger UI para probar endpoints protegidos
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer. Escribe: Bearer {token}",
        Name        = "Authorization",
        In          = ParameterLocation.Header,
        Type        = SecuritySchemeType.ApiKey,
        Scheme      = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// -------------------------------------------------------
// 6. Middleware pipeline
// -------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DestinoPeru API v1"));
}

if (!app.Configuration.GetValue<bool>("DisableHttpsRedirection"))
    app.UseHttpsRedirection();
app.UseCors("BlazorPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// -------------------------------------------------------
// 7. Aplicar migraciones automáticamente al iniciar
// -------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();