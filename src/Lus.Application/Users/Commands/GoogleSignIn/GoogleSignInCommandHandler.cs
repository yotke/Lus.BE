using IdentityModel;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Lus.Application.Users.Entities;
using Lus.Application.Users.Repositories;

namespace Lus.Application.Users.Commands.GoogleSignIn
{
    public class GoogleSignInCommandHandler : IRequestHandler<GoogleSignInCommand, string>
    {
        private readonly IUsersRepository usersRepository;
        private readonly IPasswordHasher<User> passwordHasher;

        public GoogleSignInCommandHandler(IUsersRepository usersRepository, IPasswordHasher<User> passwordHasher)
        {
            this.usersRepository = usersRepository;
            this.passwordHasher = passwordHasher;
        }

        public async Task<string> Handle(GoogleSignInCommand request, CancellationToken cancellationToken)
        {
            var email = request.Email.ToLowerInvariant();

            var existing = await this.usersRepository.GetAsync(u => u.UserName == email);
            if (existing != null)
            {
                return email;
            }

            // Google already verified the e-mail, so the local account is created
            // confirmed. A random password is stored so the account can only be
            // used through Google until the user sets one via "forgot password".
            var randomPassword = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

            var user = new User
            {
                Email = email,
                UserName = email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsConfirmed = true,
                PasswordChangedDate = DateTime.UtcNow,
                AllowedScopes = new List<string>
                {
                    ApplicationConstants.Scopes.PublicApiScope,
                    ApplicationConstants.Scopes.InternalApi
                },
                ClientSecrets = new List<string> { randomPassword.ToSha256() },
                AllowedGrantTypes = new List<string> { ApplicationConstants.AllowedGrandTypes.Password },
                Claims = new List<KeyValuePair<string, string>>
                {
                    new(ApplicationConstants.ClaimsTypes.Allowance, "secure_data_read"),
                    new(ApplicationConstants.ClaimsTypes.Scope, ApplicationConstants.Scopes.InternalApi)
                }
            };

            user.PasswordHash = this.passwordHasher.HashPassword(user, randomPassword);

            await this.usersRepository.AddAsync(user, cancellationToken);

            return email;
        }
    }
}
