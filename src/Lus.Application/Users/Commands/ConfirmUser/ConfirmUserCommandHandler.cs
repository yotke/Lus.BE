using AutoMapper;
using MediatR;
using Lus.Application.Users.Repositories;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Commands.ConfirmUser
{
    public class ConfirmUserCommandHandler : IRequestHandler<ConfirmUserCommand, UserDto>
    {
        private readonly IUsersRepository usersRepository;
        private readonly IMapper mapper;

        public ConfirmUserCommandHandler(IUsersRepository usersRepository, IMapper mapper)
        {
            this.mapper = mapper;
            this.usersRepository = usersRepository;
        }

        public async Task<UserDto> Handle(ConfirmUserCommand request, CancellationToken cancellationToken)
        {
            var user = await this.usersRepository.GetAsync(u => u.ConfirmationToken == request.ConfirmToken, cancellationToken);
            if (user != null && user.VerificationTokenExpiration > DateTime.UtcNow)
            {
                user.ConfirmationToken = null;
                user.VerificationTokenExpiration = null;
                user.IsConfirmed = true;

                await this.usersRepository.UpdateUserAsync(user);

                return this.mapper.Map<UserDto>(user);
            }

            return null;
        }
    }
}
