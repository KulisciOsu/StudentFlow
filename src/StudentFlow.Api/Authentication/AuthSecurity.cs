using System.Globalization;
using System.Threading.RateLimiting;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace StudentFlow.Api.Authentication;

/// <summary>Společná konfigurace autentizace a ochrany požadavků API.</summary>
public static class AuthSecurity
{
    /// <summary>Zaregistruje session, CSRF, hashování, limity a jednotné validační chyby.</summary>
    /// <remarks>Výchozí autorizační politika vyžaduje přihlášení, pokud endpoint nemá AllowAnonymous.</remarks>
    public static void AddAuthSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "__Host-StudentFlow.Csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.Path = "/";
        });
        services.AddScoped<CsrfValidationFilter>();
        services.AddScoped<AuthBodyLimitFilter>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<LoginPasswordVerifier>();
        services.AddAuthentication(SessionToken.Scheme).AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionToken.Scheme, _ => { });
        services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        services.Configure<PasswordHasherOptions>(options => options.IterationCount = 220_000);
        services.AddScoped<IPasswordHasher<object>, PasswordHasher<object>>();
        services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
            options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressMapClientErrors = true;
            options.InvalidModelStateResponseFactory = action =>
            {
                if (action.ModelState.Values.SelectMany(value => value.Errors)
                    .Any(error => error.Exception?.GetBaseException() is BadHttpRequestException { StatusCode: 413 }))
                    return ApiProblems.Result(action.HttpContext, 413, "PAYLOAD_TOO_LARGE", "Požadavek je příliš velký", "Zadané údaje překračují povolenou velikost.");
                return ApiProblems.Result(action.HttpContext, 400, "VALIDATION_FAILED", "Neplatné vstupní údaje",
                    "Zkontrolujte zadané údaje a formát požadavku.", new Dictionary<string, string[]>
                    {
                        ["request"] = ["Požadavek musí obsahovat pouze povolená pole ve správném formátu."]
                    });
            };
        });

        services.AddRateLimiter(options =>
        {
            AddLimit(options, configuration, "auth-register", "RegistrationRateLimit", 20, 60);
            AddLimit(options, configuration, "auth-login", "LoginRateLimit", 30, 15);
            options.OnRejected = async (context, _) =>
            {
                var seconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                    ? Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)) : 60;
                context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
                await ApiProblems.WriteAsync(context.HttpContext, 429, "RATE_LIMITED", "Příliš mnoho pokusů", "Zkuste to znovu později.");
            };
        });
    }

    private static void AddLimit(RateLimiterOptions options, IConfiguration configuration, string policy, string section, int defaultPermits, int defaultMinutes)
    {
        var permits = configuration.GetValue($"Auth:{section}:PermitLimit", defaultPermits);
        var minutes = configuration.GetValue($"Auth:{section}:WindowMinutes", defaultMinutes);
        if (permits <= 0 || minutes <= 0) throw new InvalidOperationException("Auth rate limits must be positive.");
        options.AddPolicy(policy, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permits, Window = TimeSpan.FromMinutes(minutes), QueueLimit = 0, AutoReplenishment = true
            }));
    }
}
