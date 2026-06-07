using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Lus.Application;
using Lus.Application.Common.Services;
using Lus.Application.Users;
using Lus.Application.Users.Commands.CreateUser;
using Lus.Application.Users.Entities;
using Lus.Authorization.Authentication;
using Lus.Authorization.Csrf;
using Lus.Infrastructure.ErrorHandlers;
using Lus.Infrastructure.Extensions;
using Lus.Infrastructure.IdentityServer;
using Lus.Infrastructure.Services;
using Lus.NotificationCenter.Extensions;
using Serilog;

namespace Lus
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
        }

        public IConfiguration Configuration { get; }

        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithConfiguration()
                .AddAutoMapper(typeof(Startup), typeof(UserMappingProfile))
                .AddMediatR(typeof(CreateUserCommandHandler), typeof(Startup));


            services.AddDatabaseContext(Configuration);

            services
                .AddOptions()
                .AddEntityUpdatesConfiguration()
                .AddScoped<IUsersService, UsersService>()
                .AddScoped<IPasswordHasher<User>, PasswordHasher<User>>()
                .AddSingleton<IUserLoginAttemptsAuditService, UserLoginAttemptsAuditService>();

            ConfigureAuthentication(services);
            services.AddCookieAuthRuntimeServices();
            services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-CSRF-Token";
                options.Cookie.Name = "lus_xsrf";
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = ParseSameSite(Configuration.GetValue<string>("Auth:Cookie:SameSite") ?? "Lax");
                options.Cookie.SecurePolicy = Configuration.GetValue("Auth:Cookie:Secure", false)
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.SameAsRequest;
            });

            services
                .AddCaching(Configuration)
                .AddRateLimiter(Configuration)
                .AddHttpClientsConfiguration(Configuration)
                .AddHttpContextAccessor()
                .AddUserAccessor(Configuration)
                .AddNotificationCenter(Configuration)
                .AddSwaggerConfiguration(Configuration)
                .AddSecurityConfiguration(Configuration)
                .AddOptionsConfiguration(Configuration)
                .AddRepositories()
                .AddRetrievers();

            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromSeconds(10);
                options.Cookie.HttpOnly = false;
                options.Cookie.IsEssential = true;
                options.Cookie.Name = "lus_session";
                var cookieDomain = Configuration.GetValue<string>("Auth:Cookie:Domain");
                if (!string.IsNullOrWhiteSpace(cookieDomain))
                {
                    options.Cookie.Domain = cookieDomain;
                }
                options.Cookie.SameSite = ParseSameSite(Configuration.GetValue<string>("Auth:Cookie:SameSite") ?? "Lax");
                options.Cookie.SecurePolicy = Configuration.GetValue("Auth:Cookie:Secure", false)
                    ? CookieSecurePolicy.Always
                    : CookieSecurePolicy.SameAsRequest;
            });
            services.AddHealthChecks(); // Adds the health checks services
            
            services.AddSignalR(hubOptions =>
            {
                hubOptions.MaximumReceiveMessageSize = null; // no limit
                hubOptions.EnableDetailedErrors = true;
            }).AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = null;
            });
        }

        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //app.UseRateLimiter();
            //app.UseIPFilter();

            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                ExceptionHandler = SystemExceptionHandler.UnhandledExceptionsHandler
            });

            app.UseHangfireDashboard();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lus API V1");
            });

            
            app.UseWebSockets();

            app.UseRouting();
            
            app.UseCors("DefaultCorsPolicy");
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSession();

            app.Map("/csrf-token", builder =>
            {
                builder.Run(context =>
                {
                    var csrf = context.RequestServices.GetRequiredService<ICsrfTokenService>();
                    var token = csrf.GetAndStoreToken(context);
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsJsonAsync(new { token });
                });
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.WithSwaggerRedirection(Configuration);
                endpoints.MapDefaultControllerRoute();
                endpoints.MapSignalRHubs();
                endpoints.MapHealthChecks("/health"); // Adds the health check endpoint
            });

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.ApplyMigrations(Configuration);

            Seed(app);
            CreateJobs(app);
        }

        protected virtual void ConfigureAuthentication(IServiceCollection services) =>
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthSchemes.Smart;
                options.DefaultAuthenticateScheme = CookieAuthSchemes.Smart;
                options.DefaultChallengeScheme = CookieAuthSchemes.Smart;
            })
                .AddPolicyScheme(CookieAuthSchemes.Smart, CookieAuthSchemes.Smart, options =>
                {
                    // First-party SPA uses cookies; service-to-service callers send a Basic/Bearer
                    // header and explicitly opt into the BasicAuthentication scheme on their endpoints.
                    options.ForwardDefaultSelector = _ => CookieAuthSchemes.Api;
                })
                .AddCookie(CookieAuthSchemes.IdentityServer, options =>
                {
                    ConfigureCookie(options, $"{Configuration.GetValue<string>("Auth:Cookie:Name", "lus_sid")}_idsrv");
                })
                .AddCookie(CookieAuthSchemes.Api, options =>
                {
                    ConfigureCookie(options, Configuration.GetValue<string>("Auth:Cookie:Name", "lus_sid"));
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnRedirectToLogin = context =>
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return Task.CompletedTask;
                        },
                        OnRedirectToAccessDenied = context =>
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            return Task.CompletedTask;
                        }
                    };
                });

        private void ConfigureCookie(CookieAuthenticationOptions options, string cookieName)
        {
            options.Cookie.Name = cookieName;
            options.Cookie.Path = "/";
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = ParseSameSite(Configuration.GetValue<string>("Auth:Cookie:SameSite") ?? "Lax");
            options.Cookie.SecurePolicy = Configuration.GetValue("Auth:Cookie:Secure", false)
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
            var domain = Configuration.GetValue<string>("Auth:Cookie:Domain");
            if (!string.IsNullOrWhiteSpace(domain))
            {
                options.Cookie.Domain = domain;
            }
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(Configuration.GetValue("Auth:Cookie:SessionTimeoutHours", 8));
        }

        private static SameSiteMode ParseSameSite(string value) =>
            value.Trim().ToLowerInvariant() switch
            {
                "none" => SameSiteMode.None,
                "strict" => SameSiteMode.Strict,
                _ => SameSiteMode.Lax
            };

        protected virtual void Seed(IApplicationBuilder applicationBuilder) =>
            applicationBuilder.SeedClients(Configuration);

        protected virtual void CreateJobs(IApplicationBuilder applicationBuilder) =>
            applicationBuilder.CreateJobs(Configuration);
    }
}
