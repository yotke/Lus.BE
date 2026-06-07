using IdentityModel;
using Lus.Application.Users.Entities;
using Lus.Application;
using Lus.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace Lus.Infrastructure.Extensions
{
    public static class ClientsSeedExtensions
    {
        public static void SeedClients(this IApplicationBuilder app, IConfiguration configuration)
        {
            using (var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
                var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
                var seedData = configuration.GetSection("IdentityServer:DefaultClients").Get<Dictionary<string, SeedOption>>();
                foreach (var seedOption in seedData)
                {
                    TryAddUser(context, passwordHasher, seedOption);
                }
            }
        }

        private static void TryAddUser(ApplicationContext context, IPasswordHasher<User> passwordHasher, KeyValuePair<string, SeedOption> seedOption)
        {
            var user = context.Users.FirstOrDefault(u => u.UserName == seedOption.Key);
            if (user == null)
            {
                context.Users.Add(ToUser(passwordHasher, seedOption));
                context.SaveChanges();
            }
            else
            {
                var userToSeed = ToUser(passwordHasher, seedOption);
                if (user.ClientSecrets.All(cs => userToSeed.ClientSecrets.Any(scs => cs != scs)))
                {
                    user.ClientSecrets = userToSeed.ClientSecrets;
                    context.SaveChanges();
                }
            }
        }

        private static string SecretShaProvider(string secret) => secret.ToSha256();

        private static User ToUser(IPasswordHasher<User> passwordHasher, KeyValuePair<string, SeedOption> userData)
        {
            var userName = userData.Key;
            var data = userData.Value;
            var claims = CreateClaims(data);
            var now = DateTime.UtcNow;
            var user = new User
            {
                UserName = userName,
                Email = userName,
                FirstName = data.FirstName,
                LastName = data.LastName,
                IdNumber = data.IdNumber,
                ClientSecrets = new List<string> { SecretShaProvider(data.Secret) },
                Claims = claims,
                AllowedGrantTypes = data.AllowedGrandTypes,
                AllowedScopes = data.Scopes,
                Active = true,
                IsConfirmed = true,
                CreatedById = -1,
                UpdatedById = -1,
                CreatedOn = now,
                UpdatedOn = now
            };

            user.PasswordHash = passwordHasher.HashPassword(user, data.Secret);
            return user;
        }

        private static ICollection<KeyValuePair<string, string>> CreateClaims(SeedOption data)
        {
            var claims = (data.Claims ?? new Dictionary<string, string>()).ToList();
            var permissions =
                data.Permission?.Select(p =>
                    new KeyValuePair<string, string>(ApplicationConstants.ClaimsTypes.Permission, p)) ??
                Enumerable.Empty<KeyValuePair<string, string>>();
            claims.AddRange(permissions);

            return claims;
        }

        private class SeedOption
        {
            public List<string> Scopes { get; set; }

            public List<string> AllowedGrandTypes { get; set; }

            public Dictionary<string, string> Claims { get; set; }

            public List<string> Permission { get; set; }

            public string FirstName { get; set; }

            public string IdNumber { get; set; }

            public string LastName { get; set; }

            public string Secret { get; set; }
        }
    }
}
