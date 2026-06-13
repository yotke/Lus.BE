using IdentityModel;
using Lus.Application;
using Lus.Application.Roles.Entities;
using Lus.Application.Users.Entities;
using Lus.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lus.Infrastructure.Extensions
{
    /// <summary>
    /// Seeds the baseline RBAC data the platform needs to be usable on an empty
    /// database: the global roles and a default administrator that can sign in.
    /// Everything here is idempotent, so it is safe to run on every startup.
    ///
    /// The role names intentionally mirror <see cref="ApplicationConstants.ClaimsValues"/>
    /// so that the global "SITEADMIN"/"DEVELOPER" bypass in
    /// <c>PermissionRequirementHandler</c> recognises the seeded admin.
    /// </summary>
    public static class IdentitySeedExtensions
    {
        public static void SeedIdentity(this IApplicationBuilder app, IConfiguration configuration)
        {
            using var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

            var roles = SeedRoles(context, configuration);
            SeedDefaultAdmin(context, passwordHasher, configuration, roles);
        }

        private static IReadOnlyDictionary<string, Role> SeedRoles(ApplicationContext context, IConfiguration configuration)
        {
            var seedRoles = configuration.GetSection("IdentitySeed:Roles").Get<List<RoleSeedOption>>();
            if (seedRoles == null || seedRoles.Count == 0)
            {
                seedRoles = DefaultRoles();
            }

            var now = DateTime.UtcNow;
            var existingGlobalRoleNames = context.Roles
                .Where(r => r.OrganizationId == null)
                .Select(r => r.Name)
                .ToList();

            foreach (var roleOption in seedRoles)
            {
                if (string.IsNullOrWhiteSpace(roleOption.Name) ||
                    existingGlobalRoleNames.Contains(roleOption.Name))
                {
                    continue;
                }

                context.Roles.Add(new Role
                {
                    Name = roleOption.Name,
                    HebrewName = roleOption.HebrewName,
                    Immunity = roleOption.Immunity,
                    ShowToAdmin = roleOption.ShowToAdmin,
                    OrganizationId = null,
                    Active = true,
                    CreatedById = -1,
                    UpdatedById = -1,
                    CreatedOn = now,
                    UpdatedOn = now
                });
                existingGlobalRoleNames.Add(roleOption.Name);
            }

            context.SaveChanges();

            return context.Roles
                .Where(r => r.OrganizationId == null)
                .ToList()
                .GroupBy(r => r.Name)
                .ToDictionary(g => g.Key, g => g.First());
        }

        private static void SeedDefaultAdmin(
            ApplicationContext context,
            IPasswordHasher<User> passwordHasher,
            IConfiguration configuration,
            IReadOnlyDictionary<string, Role> roles)
        {
            var admin = configuration.GetSection("IdentitySeed:DefaultAdmin").Get<DefaultAdminOption>() ?? DefaultAdmin();
            if (admin == null || string.IsNullOrWhiteSpace(admin.Email) || string.IsNullOrWhiteSpace(admin.Password))
            {
                return;
            }

            var email = admin.Email.ToLowerInvariant();
            var now = DateTime.UtcNow;

            // IgnoreQueryFilters so a previously soft-deleted admin is still found.
            var user = context.Users.IgnoreQueryFilters().FirstOrDefault(u => u.UserName == email);
            if (user == null)
            {
                user = new User
                {
                    UserName = email,
                    Email = email,
                    FirstName = admin.FirstName,
                    LastName = admin.LastName,
                    IdNumber = admin.IdNumber,
                    IsConfirmed = true,
                    Active = true,
                    PasswordChangedDate = now,
                    AllowedScopes = new List<string>
                    {
                        ApplicationConstants.Scopes.InternalApi,
                        ApplicationConstants.Scopes.PublicApiScope
                    },
                    AllowedGrantTypes = new List<string> { ApplicationConstants.AllowedGrandTypes.Password },
                    ClientSecrets = new List<string> { admin.Password.ToSha256() },
                    Claims = BuildAdminClaims(),
                    CreatedById = -1,
                    UpdatedById = -1,
                    CreatedOn = now,
                    UpdatedOn = now
                };
                user.PasswordHash = passwordHasher.HashPassword(user, admin.Password);

                context.Users.Add(user);
                context.SaveChanges();
            }
            else
            {
                // The user already exists: guarantee it can always sign in by
                // re-asserting a confirmed/active account with the configured
                // password. This recovers accounts that were left unconfirmed,
                // soft-deleted, or with a stale password.
                user.IsConfirmed = true;
                user.Active = true;
                user.DeletedOn = null;
                user.DeletedById = null;
                user.PasswordChangedDate = now;
                user.ClientSecrets = new List<string> { admin.Password.ToSha256() };
                user.PasswordHash = passwordHasher.HashPassword(user, admin.Password);
                user.UpdatedById = -1;
                user.UpdatedOn = now;
                context.SaveChanges();
            }

            var desiredRoleNames = (admin.Roles != null && admin.Roles.Count > 0)
                ? admin.Roles
                : new List<string> { ApplicationConstants.ClaimsValues.SiteAdmin };

            var existingRoleIds = context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.RoleId)
                .ToList();

            var added = false;
            foreach (var roleName in desiredRoleNames)
            {
                if (!roles.TryGetValue(roleName, out var role))
                {
                    continue;
                }

                if (existingRoleIds.Contains(role.Id))
                {
                    continue;
                }

                context.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    Active = true,
                    CreatedById = -1,
                    UpdatedById = -1,
                    CreatedOn = now,
                    UpdatedOn = now
                });
                existingRoleIds.Add(role.Id);
                added = true;
            }

            if (added)
            {
                context.SaveChanges();
            }
        }

        private static ICollection<KeyValuePair<string, string>> BuildAdminClaims() => new List<KeyValuePair<string, string>>
        {
            new(ApplicationConstants.ClaimsTypes.Allowance, "secure_data_read"),
            new(ApplicationConstants.ClaimsTypes.Allowance, ApplicationConstants.Allowance.DeleteUser),
            new(ApplicationConstants.ClaimsTypes.Scope, ApplicationConstants.Scopes.InternalApi)
        };

        /// <summary>
        /// Smart default role hierarchy for the Shiftiz platform (shiftiz.com).
        /// Higher <c>Immunity</c> means a more powerful role. The first three
        /// names must stay aligned with <see cref="ApplicationConstants.ClaimsValues"/>.
        /// </summary>
        private static List<RoleSeedOption> DefaultRoles() => new()
        {
            new RoleSeedOption { Name = ApplicationConstants.ClaimsValues.SiteAdmin, HebrewName = "מנהל מערכת", Immunity = 100, ShowToAdmin = true },
            new RoleSeedOption { Name = ApplicationConstants.ClaimsValues.Developer, HebrewName = "מפתח", Immunity = 95, ShowToAdmin = false },
            new RoleSeedOption { Name = ApplicationConstants.ClaimsValues.Admin, HebrewName = "מנהל", Immunity = 90, ShowToAdmin = true },
            new RoleSeedOption { Name = ApplicationConstants.ClaimsValues.CommitteesManager, HebrewName = "מנהל ועדות", Immunity = 80, ShowToAdmin = true },
            new RoleSeedOption { Name = "ORGANIZATION_OWNER", HebrewName = "בעל ארגון", Immunity = 70, ShowToAdmin = true },
            new RoleSeedOption { Name = "ORGANIZATION_ADMIN", HebrewName = "מנהל ארגון", Immunity = 60, ShowToAdmin = true },
            new RoleSeedOption { Name = "MANAGER", HebrewName = "מנהל משמרות", Immunity = 50, ShowToAdmin = true },
            new RoleSeedOption { Name = "EMPLOYEE", HebrewName = "עובד", Immunity = 10, ShowToAdmin = true }
        };

        private static DefaultAdminOption DefaultAdmin() => new()
        {
            Email = "admin@shiftiz.com",
            // Dev placeholder only — override via IdentitySeed:DefaultAdmin:Password
            // (appsettings/env) and change it after the first login.
            Password = "Shiftiz!2026",
            FirstName = "Site",
            LastName = "Admin",
            IdNumber = "000000000",
            Roles = new List<string> { ApplicationConstants.ClaimsValues.SiteAdmin }
        };

        private sealed class RoleSeedOption
        {
            public string Name { get; set; }

            public string HebrewName { get; set; }

            public int Immunity { get; set; }

            public bool ShowToAdmin { get; set; }
        }

        private sealed class DefaultAdminOption
        {
            public string Email { get; set; }

            public string Password { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }

            public string IdNumber { get; set; }

            public List<string> Roles { get; set; }
        }
    }
}
