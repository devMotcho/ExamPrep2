using Auth.Application.Interfaces;
using Auth.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using ExamPrep.Shared.Constants;

namespace Auth.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services)
    {
        // Register concrete RsaKeyProvider and expose it as IJwksProvider.
        // Single instance so key material is loaded once at startup.
        services.AddSingleton<RsaKeyProvider>();
        services.AddSingleton<IJwksProvider>(sp => sp.GetRequiredService<RsaKeyProvider>());

        services
            .AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        // Wire up the RSA public key and JWT settings via PostConfigure so that
        // overrides from WebApplicationFactory.ConfigureWebHost are already applied.
        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .PostConfigure<IJwksProvider, IConfiguration>((options, keys, cfg) =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = cfg[ConfigKeys.Jwt.Issuer],
                    ValidAudience = cfg[ConfigKeys.Jwt.Audience],
                    IssuerSigningKey = new RsaSecurityKey(keys.PublicKey) { KeyId = keys.KeyId },
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var jti = context.Principal?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
                        if (!string.IsNullOrEmpty(jti))
                        {
                            var blocklistService = context.HttpContext.RequestServices.GetRequiredService<IJwtBlocklistService>();
                            if (await blocklistService.IsTokenBlockedAsync(jti))
                            {
                                context.Fail("This token has been revoked.");
                            }
                        }
                    }
                };
            });

        return services;
    }
}
