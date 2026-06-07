using MediatR;
using Lus.Application.Common.Extensions;
using Lus.Application.Common.Ports;
using Lus.Application.Users.Repositories;
using Lus.Contracts.Users;
using Lus.Infrastructure.Exceptions;

namespace Lus.Application.Users.Commands.SsoCheckUser
{
    public class SsoCheckUserCommandHandler : IRequestHandler<SsoCheckUserCommand, UserCheckedDto>
    {
        private readonly IUsersRepository usersRepository;
        private readonly IRecaptchaAdapter recaptchaAdapter;

        public SsoCheckUserCommandHandler(IUsersRepository usersRepository, IRecaptchaAdapter recaptchaAdapter)
        {
            this.usersRepository = usersRepository;
            this.recaptchaAdapter = recaptchaAdapter;
        }

        public async Task<UserCheckedDto> Handle(SsoCheckUserCommand request, CancellationToken cancellationToken)
        {
            var result = await this.recaptchaAdapter.CheckRecaptcha(cancellationToken);
            if (!result)
            {
                throw new MembershipException("Recaptcha not valid", 41);
            }

            var user = await this.usersRepository.GetAsync(u => u.Email == request.Email, cancellationToken);

            return new UserCheckedDto
            {
                Email = user?.Email ?? request.Email,
                Phone = (user?.Phone ?? string.Empty).HidePhoneNumber()
            };
        }
    }
}
