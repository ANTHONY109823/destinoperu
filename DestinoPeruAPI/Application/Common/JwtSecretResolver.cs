namespace DestinoPeruAPI.Application.Common;

public static class JwtSecretResolver
{
    private const int MinLength = 32;

    public static string Resolve(IConfiguration configuration, IHostEnvironment environment)
    {
        var fromEnv = Environment.GetEnvironmentVariable("JwtSettings__SecretKey");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return ValidateLength(fromEnv.Trim(), "JwtSettings__SecretKey (variable de entorno)");

        var fromConfig = configuration["JwtSettings:SecretKey"]?.Trim();
        var isPlaceholder = string.IsNullOrWhiteSpace(fromConfig)
            || fromConfig.Contains("CAMBIAR", StringComparison.OrdinalIgnoreCase);

        if (environment.IsProduction())
        {
            throw new InvalidOperationException(
                "JWT no configurado en producción. En Railway (servicio API) define la variable de entorno " +
                "JwtSettings__SecretKey con al menos 32 caracteres aleatorios. No uses la clave del repositorio.");
        }

        if (!isPlaceholder && fromConfig!.Length >= MinLength)
            return fromConfig;

        throw new InvalidOperationException(
            "JWT no configurado para desarrollo local. Define JwtSettings__SecretKey (mín. 32 caracteres) " +
            "o JwtSettings:SecretKey en appsettings.Development.json (no commitear secretos reales).");
    }

    private static string ValidateLength(string secret, string source)
    {
        if (secret.Length < MinLength)
            throw new InvalidOperationException($"{source} debe tener al menos {MinLength} caracteres.");
        return secret;
    }
}
