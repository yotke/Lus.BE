using AutoMapper;
using MediatR;
using Lus.Application.Users.Repositories;
using Lus.Infrastructure.Exceptions;

namespace Lus.Application.Users.Commands.CheckEmailToken
{
    public class CheckEmailTokenCommandHandler : IRequestHandler<CheckEmailTokenCommand, Unit>
    {
        private readonly IUsersRepository usersRepository;
        private readonly IMapper mapper;

        public CheckEmailTokenCommandHandler(IUsersRepository usersRepository, IMapper mapper)
        {
            this.mapper = mapper;
            this.usersRepository = usersRepository;
        }

        public async Task<Unit> Handle(CheckEmailTokenCommand request, CancellationToken cancellationToken)
        {
            var user = await this.usersRepository.GetAsync(u => u.PasswordVerificationToken == request.EmailToken, cancellationToken);
            if (user == null || !(user.VerificationTokenExpiration > DateTime.UtcNow))
            {
                throw new MembershipException(25);
            }

            return Unit.Value;
        }
    }
}
