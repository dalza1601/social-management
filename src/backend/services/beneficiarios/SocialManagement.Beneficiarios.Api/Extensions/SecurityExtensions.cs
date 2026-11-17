using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SocialManagement.Beneficiarios.Api.Security;
using SocialManagement.Beneficiarios.Application.Interfaces;
using SocialManagement.Beneficiarios.Infrastructure.Configurations;

namespace SocialManagement.Beneficiarios.Api.Extensions;

/// <summary>
/// Seguridad de la API (sesión 04): autenticación JWT Bearer, roles y políticas de autorización,
/// usuario actual para la auditoría y emisión de tokens solo para desarrollo.
/// </summary>
public static class SecurityExtensions
{
    public static IServiceCollection AddJwtSecurity(this IServiceCollection services)
    {
        AddJwtOptions(services);
        AddDevelopmentTokenOptions(services);

        // Esquema "Bearer": lee "Authorization: Bearer {token}" y valida firma, emisor, audiencia y expiración.
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;

                // Claims con su nombre corto del JWT ("role", "name"), sin convertirlos a URIs de .NET.
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = jwt.HasValidSigningKey ? jwt.CreateSigningKey() : null,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    // Tolerancia por diferencias de reloj entre servidores (por defecto serían 5 minutos).
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtClaimTypes.Name,
                    RoleClaimType = JwtClaimTypes.Role,
                };
            });

        // Políticas explícitas por tipo de operación (ver AuthorizationPolicies).
        services.AddAuthorizationBuilder().AddBeneficiariosPolicies();

        // 401 y 403 de los controllers en formato ApiResponse.
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiResponseAuthorizationResultHandler>();

        // Usuario de auditoría (CreatedBy / UpdatedBy) = usuario del token. Reemplaza el "sin usuario" de Infrastructure.
        services.AddHttpContextAccessor();
        services.Replace(ServiceDescriptor.Singleton<ICurrentUser, HttpContextCurrentUser>());

        services.AddSingleton<DevelopmentTokenIssuer>();

        return services;
    }

    /// <summary>
    /// ¿Se publica <c>POST /api/auth/dev-token</c>? Solo en el ambiente Development y con DevelopmentTokens:Enabled.
    /// (Si alguien lo activa en otro ambiente, la API ni siquiera arranca: ver AddDevelopmentTokenOptions.)
    /// </summary>
    public static bool IsDevelopmentTokenIssuerEnabled(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.Environment.IsDevelopment()
            && app.Services.GetRequiredService<IOptions<DevelopmentTokenOptions>>().Value.Enabled;
    }

    private static void AddJwtOptions(IServiceCollection services)
    {
        const string section = JwtOptions.SectionName;

        services.AddOptions<JwtOptions>()
            .BindConfiguration(section)
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), ConfigurationMessages.Required($"{section}:Issuer"))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), ConfigurationMessages.Required($"{section}:Audience"))
            .Validate(o => !string.IsNullOrWhiteSpace(o.SigningKey), ConfigurationMessages.Required($"{section}:SigningKey"))
            .Validate(
                o => string.IsNullOrWhiteSpace(o.SigningKey) || o.HasValidSigningKey,
                ConfigurationMessages.Invalid($"{section}:SigningKey", $"debe tener al menos {JwtOptions.MinimumSigningKeyBytes} bytes (256 bits) para HMAC-SHA256"))
            .Validate(
                o => o.ExpirationMinutes is > 0 and <= 1440,
                ConfigurationMessages.Invalid($"{section}:ExpirationMinutes", "debe estar entre 1 y 1440 minutos"))
            .ValidateOnStart();
    }

    private static void AddDevelopmentTokenOptions(IServiceCollection services)
    {
        const string section = DevelopmentTokenOptions.SectionName;

        services.AddOptions<DevelopmentTokenOptions>()
            .BindConfiguration(section)
            .Validate<IHostEnvironment>(
                (o, environment) => !o.Enabled || environment.IsDevelopment(),
                $"'{section}:Enabled' solo puede ser true en el ambiente Development: la emisión de tokens de " +
                "demostración no existe en Staging ni en Production.")
            .Validate(
                o => !o.Enabled || o.Users.Count > 0,
                ConfigurationMessages.Required($"{section}:Users:0:UserName") +
                " La emisión de tokens de desarrollo está activada pero no hay usuarios demo configurados.")
            .Validate(
                o => !o.Enabled || o.Users.All(u => !string.IsNullOrWhiteSpace(u.UserName) && u.UserName.Length <= 100),
                ConfigurationMessages.Invalid($"{section}:Users:N:UserName", "cada usuario demo necesita un nombre de hasta 100 caracteres"))
            .Validate(
                o => !o.Enabled || o.Users.Select(u => u.UserName).Distinct(StringComparer.Ordinal).Count() == o.Users.Count,
                ConfigurationMessages.Invalid($"{section}:Users", "hay nombres de usuario repetidos"))
            .Validate(
                o => !o.Enabled || o.Users.All(u => (u.Password ?? string.Empty).Length >= DevelopmentTokenOptions.MinimumPasswordLength),
                ConfigurationMessages.Invalid($"{section}:Users:N:Password", $"cada clave demo debe tener al menos {DevelopmentTokenOptions.MinimumPasswordLength} caracteres"))
            .Validate(
                o => !o.Enabled || o.Users.All(u => u.Roles.Count > 0 && u.Roles.All(role => Roles.Todos.Contains(role))),
                ConfigurationMessages.Invalid($"{section}:Users:N:Roles", $"cada usuario demo necesita uno o más roles válidos: {string.Join(", ", Roles.Todos)}"))
            .ValidateOnStart();
    }
}
