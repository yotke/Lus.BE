using IdentityModel;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Lus.Application.Common.Ports;
using Lus.Application.Users.Entities;
using Lus.Application.Users.Repositories;
using Lus.Infrastructure.Exceptions;

namespace Lus.Application.Users.Commands.UserResetPasswordFromSms
{
    public class UserResetPasswordFromSmsCommandHandler : IRequestHandler<UserResetPasswordFromSmsCommand, Unit>
    {
        private readonly IUsersRepository usersRepository;
        private readonly IUserPasswordHistoriesRepository userPasswordHistoriesRepository;
        private readonly IRecaptchaAdapter recaptchaAdapter;
        private readonly IPasswordHasher<User> passwordHasher;

        public UserResetPasswordFromSmsCommandHandler(IUserPasswordHistoriesRepository userPasswordHistoriesRepository, IUsersRepository usersRepository, IPasswordHasher<User> passwordHasher, IRecaptchaAdapter recaptchaAdapter)
        {
            this.userPasswordHistoriesRepository = userPasswordHistoriesRepository;
            this.passwordHasher = passwordHasher;
            this.usersRepository = usersRepository;
            this.recaptchaAdapter = recaptchaAdapter;
        }

        public async Task<Unit> Handle(UserResetPasswordFromSmsCommand command, CancellationToken cancellationToken)
        {
            var user = await ValidateUser(command, cancellationToken);

            if (user.UserPasswordHistories.Any(p => this.passwordHasher.VerifyHashedPassword(user, p.PasswordHash, command.Password) == PasswordVerificationResult.Success))
            {
                throw new MembershipException(14);
            }

            await this.userPasswordHistoriesRepository.AddAsync(new UserPasswordHistory
            {
                PasswordHash = user.PasswordHash,
                UserId = user.Id
            }, cancellationToken);

            user.PasswordHash = this.passwordHasher.HashPassword(user, command.Password);
            user.PasswordChangedDate = DateTime.Now;
            user.ClientSecrets = new List<string> { command.Password.ToSha256() };

            await this.usersRepository.UpdateAsync(user, cancellationToken);

            return Unit.Value;
        }

        private async Task<User> ValidateUser(UserResetPasswordFromSmsCommand command, CancellationToken cancellationToken)
        {
            var result = await this.recaptchaAdapter.CheckRecaptcha(cancellationToken);
            if (!result)
            {
                throw new MembershipException("Recaptcha not valid", 41);
            }

            var user = await this.usersRepository.GetWithIncludeAsync(u => u.Email == command.Email, cancellationToken, u => u.UserPasswordHistories);
            if (user == null)
            {
                throw new MembershipException(18);
            }

            if (user.SmsVerificationToken != command.SmsCode || !(user.SmsTokenExpiration > DateTime.UtcNow))
            {
                throw new MembershipException(21);
            }

            return user;
        }
    }
}
