using MediatR;
using Microsoft.AspNetCore.Identity;
using Lus.Application.Common.Ports;
using Lus.Application.Users.Entities;
using Lus.Application.Users.Repositories;
using Lus.Authorization;
using Lus.Infrastructure.Exceptions;

namespace Lus.Application.Users.Commands.CheckPassword
{
    public class CheckPasswordCommandHandler : IRequestHandler<CheckPasswordCommand, Unit>
    {
        private readonly IUsersRepository usersRepository;
        private readonly IPasswordHasher<User> passwordHasher;
        private readonly IRecaptchaAdapter recaptchaAdapter;
        private readonly IUserAccessor userAccessor;

        public CheckPasswordCommandHandler(IUsersRepository usersRepository, IPasswordHasher<User> passwordHasher, IRecaptchaAdapter recaptchaAdapter, IUserAccessor userAccessor)
        {
            this.userAccessor = userAccessor;
            this.usersRepository = usersRepository;
            this.recaptchaAdapter = recaptchaAdapter;
            this.passwordHasher = passwordHasher;
        }

        public async Task<Unit> Handle(CheckPasswordCommand createCommand, CancellationToken cancellationToken)
        {
            var user = await ValidateUser(createCommand);

            if (this.passwordHasher.VerifyHashedPassword(user, user.PasswordHash, createCommand.Password) == PasswordVerificationResult.Failed)
            {
                throw new MembershipException(12);
            }


            return Unit.Value;
        }

        private async Task<User> ValidateUser(CheckPasswordCommand createCommand)
        {
            var result = await this.recaptchaAdapter.CheckRecaptcha();
            if (!result)
            {
                throw new MembershipException("Recaptcha not valid", 41);
            }

            var user = await this.usersRepository.GetAsync(this.userAccessor.ProjectUser.Id);
            if (user == null)
            {
                throw new MembershipException(18);
            }

            return user;
        }
    }
}
