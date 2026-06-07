using AutoMapper;
using MediatR;
using Lus.Application.Users.Repositories;
using Lus.Infrastructure.Exceptions;

namespace Lus.Application.Users.Commands.CheckUserSmsCode
{
    public class CheckUserSmsCodeCommandHandler : IRequestHandler<CheckUserSmsCodeCommand, Unit>
    {
        private readonly IUsersRepository usersRepository;
        private readonly IMapper mapper;

        public CheckUserSmsCodeCommandHandler(IUsersRepository usersRepository, IMapper mapper)
        {
            this.mapper = mapper;
            this.usersRepository = usersRepository;
        }

        public async Task<Unit> Handle(CheckUserSmsCodeCommand request, CancellationToken cancellationToken)
        {
            var user = await this.usersRepository.GetAsync(u => u.Email == request.Email && u.SmsVerificationToken == request.SmsCode, cancellationToken);
            if (user == null || !(user.SmsTokenExpiration > DateTime.UtcNow))
            {
                throw new MembershipException(21);
            }

            return Unit.Value;
        }
    }
}
