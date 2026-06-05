// ============================================================
// DestinoPeru - Program.cs
// Configuración principal — compatible con .NET 8
// ============================================================
using System.Security.Claims;
using System.Text;
using Npgsql;
using System.Threading.RateLimiting;
using DestinoPeruAPI.Application.Common;
using DestinoPeruAPI.Application.Interfaces;
using DestinoPeruAPI.Application.Services;
using Microsoft.AspNetCore.RateLimiting;
using DestinoPeruAPI.Infrastructure;
using DestinoPeruAPI.Infrastructure.Data;
using DestinoPeruAPI.Infrastructure.Dapper;
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
    // 1) URLs que Railway inyecta al vincular PostgreSQL
    foreach (var envName in new[] { "DATABASE_PRIVATE_URL", "DATABASE_URL", "DATABASE_PUBLIC_URL" })
    {
        var url = Environment.GetEnvironmentVariable(envName);
        if (!string.IsNullOrWhiteSpace(url))
            return ParsePostgresUrl(url);
    }

    // 2) Variable de entorno explicita (copiada desde el plugin Postgres)
    var envConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
    if (!string.IsNullOrWhiteSpace(envConnection))
        return NormalizeConnectionString(envConnection);

    // 3) Variables PGHOST, PGPORT, etc. (referencia al servicio Postgres en Railway)
    var fromPgVars = BuildFromPgEnvironmentVariables();
    if (fromPgVars is not null)
        return fromPgVars;

    // 4) appsettings — solo valido en desarrollo local
    var fromConfig = configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(fromConfig) && !IsProductionHostMissing(fromConfig))
        return NormalizeConnectionString(fromConfig);

    throw new InvalidOperationException(
        "PostgreSQL no configurado en Railway. En el servicio API: Settings → Connect → agrega una referencia al servicio PostgreSQL (no uses localhost).");
}

static string NormalizeConnectionString(string value) =>
    value.StartsWith("postgres", StringComparison.OrdinalIgnoreCase)
        ? ParsePostgresUrl(value)
        : value;

static bool IsProductionHostMissing(string connectionString)
{
    if (!string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Production", StringComparison.OrdinalIgnoreCase))
        return false;

    var csb = new NpgsqlConnectionStringBuilder(connectionString);
    return csb.Host is "localhost" or "127.0.0.1";
}

static string? BuildFromPgEnvironmentVariables()
{
    var host = Environment.GetEnvironmentVariable("PGHOST")
        ?? Environment.GetEnvironmentVariable("POSTGRES_HOST");
    if (string.IsNullOrWhiteSpace(host))
        return null;

    var port = int.TryParse(Environment.GetEnvironmentVariable("PGPORT"), out var p) ? p : 5432;
    return new NpgsqlConnectionStringBuilder
    {
        Host = host,
        Port = port,
        Username = Environment.GetEnvironmentVariable("PGUSER")
            ?? Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres",
        Password = Environment.GetEnvironmentVariable("PGPASSWORD")
            ?? Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "",
        Database = Environment.GetEnvironmentVariable("PGDATABASE")
            ?? Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "railway",
        SslMode = SslMode.Require
    }.ConnectionString;
}

static string ParsePostgresUrl(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    return new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.TrimStart('/').Split('?')[0],
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "",
        SslMode = SslMode.Require
    }.ConnectionString;
}

// -------------------------------------------------------
// 2. Inyección de Dependencias
// -------------------------------------------------------
builder.Services.AddSingleton<IDbConnectionFactory>(_ => new DbConnectionFactory(connectionString));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPartnerRepository, PartnerRepository>();
builder.Services.AddScoped<ITourRepository, TourRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ILoyaltyRepository, LoyaltyRepository>();

builder.Services.AddScoped<ITourQueryRepository, TourQueryRepository>();
builder.Services.AddScoped<IPartnerQueryRepository, PartnerQueryRepository>();

builder.Services.Configure<DestinoPeruAPI.Infrastructure.Media.CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddScoped<IImageService, DestinoPeruAPI.Infrastructure.Media.CloudinaryImageService>();

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TourService>();
builder.Services.AddScoped<PartnerService>();
builder.Services.AddScoped<ReservationService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<SuperAdminService>();
builder.Services.AddScoped<AgencyAdminService>();
builder.Services.AddScoped<UserAccountService>();
builder.Services.AddScoped<PopularDestinationService>();
builder.Services.AddScoped<PublicAgencyService>();
builder.Services.AddScoped<ReviewService>();

builder.Services.AddScoped<IJwtService, JwtService>();

// -------------------------------------------------------
// 3. JWT Authentication
// -------------------------------------------------------
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = JwtSecretResolver.Resolve(builder.Configuration, builder.Environment);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
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
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// -------------------------------------------------------
// 3b. Rate limiting — auth (5 intentos / IP / minuto)
// -------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// -------------------------------------------------------
// 4. CORS — politica permisiva fija (Blazor WASM en Railway)
// -------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();

        // Cuando tengas el dominio fijo del Blazor en Railway, reemplaza lo anterior por:
        // policy.WithOrigins("https://TU-BLAZOR.up.railway.app")
        //       .AllowAnyMethod()
        //       .AllowAnyHeader()
        //       .AllowCredentials();
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
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// -------------------------------------------------------
// 7. Migraciones + Seed (justo antes de app.Run — vital en Railway)
// -------------------------------------------------------
var csbLog = new NpgsqlConnectionStringBuilder(connectionString);
app.Logger.LogInformation("PostgreSQL: {Host}:{Port} / {Database}", csbLog.Host, csbLog.Port, csbLog.Database);

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var initLogger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInit");
    try
    {
        await DatabaseBootstrap.InitializeAsync(context, initLogger);
    }
    catch (Exception ex)
    {
        initLogger.LogError(ex, "Fallo al inicializar PostgreSQL (Migrate/Seed).");
        throw;
    }
}

app.Run();