using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Lus.Application;
using Lus.Authorization.Common;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Lus.Infrastructure.IdentityServer;
using Lus.Infrastructure.IdentityServer.Permissions;

namespace Lus.Infrastructure.Extensions
{
    public static class SecurityExtensions
    {
        public static IServiceCollection AddUserAccessor(this IServiceCollection services, IConfiguration configuration)
        {
            var identityConfiguration = new ServiceClientConfiguration();
            configuration.GetSection("Services:Identity").Bind(identityConfiguration);

            services.AddUserAccessor<DefaultUserAccessorFactory>(identityConfiguration, configureAction: o => o
                .WithClaimType(ClaimTypes.Email)
                .WithClaimType(ApplicationConstants.ApplicationClaimsType.ClientType)
                .WithClaimType(ApplicationConstants.ApplicationClaimsType.ClientId)
                .WithClaimType(ApplicationConstants.ApplicationClaimsType.Allowance));

            return services;
        }

        public static IServiceCollection AddSecurityConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDataProtection().SetApplicationName(configuration.GetValue<string>("General:ServiceAppName"));

            services.AddAuthentication(ApplicationConstants.AuthPolicies.BasicAuthentication).
                AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>
                    (ApplicationConstants.AuthPolicies.BasicAuthentication, null);

            services.AddAuthorization(configOptions =>
            {
                // Cookie-authenticated first-party users are trusted once authenticated.
                // Scope claims are still attached to the principal at login time so the
                // scope-based named policies below keep working for service callers.
                configOptions.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                configOptions.AddPolicy(ApplicationConstants.AuthPolicies.ReadWriteData, policy => policy
                    .Combine(configOptions.DefaultPolicy));

                configOptions.AddPolicy(ApplicationConstants.AuthPolicies.SensetiveOperation, policy => policy
                    .RequireAllowance(ApplicationConstants.Allowance.DeleteUser)
                    .Combine(configOptions.GetPolicy(ApplicationConstants.AuthPolicies.ReadWriteData)!));

                configOptions.AddPolicy(ApplicationConstants.AuthPolicies.PrivateApi,
                    policy => policy.RequireClaim("scope", ApplicationConstants.Scopes.Private, ApplicationConstants.Scopes.InternalApi));

                configOptions.AddPolicy(ApplicationConstants.AuthPolicies.ChangeUserPassword,
                    policy => policy.RequireClaim("scope", ApplicationConstants.Scopes.SecondAuthenticationFactor,
                        ApplicationConstants.Scopes.Private, ApplicationConstants.Scopes.InternalApi));
            });

            services.AddSingleton<IAuthorizationHandler, PermissionRequirementHandler>();
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            // security
            services.AddCors(options =>
            {
                var allowedOrigins = configuration.GetValue<string>("Cors:Origins");

                options.AddPolicy("DefaultCorsPolicy", builder => builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .SetIsOriginAllowed((host) => true)
                    .WithOrigins(allowedOrigins.Split(",")));
            });

            return services;
        }
    }
}
