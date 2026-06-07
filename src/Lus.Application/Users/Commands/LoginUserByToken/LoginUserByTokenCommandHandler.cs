using AutoMapper;
using MediatR;
using Lus.Application.Users.Repositories;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Commands.LoginUserByToken
{
    public class LoginUserByTokenCommandHandler : IRequestHandler<LoginUserByTokenCommand, UserDto>
    {
        private readonly IUsersRepository usersRepository;
        private readonly IMapper mapper;

        public LoginUserByTokenCommandHandler(IUsersRepository usersRepository, IMapper mapper)
        {
            this.mapper = mapper;
            this.usersRepository = usersRepository;
        }

        public async Task<UserDto> Handle(LoginUserByTokenCommand request, CancellationToken cancellationToken)
        {
            var user = await this.usersRepository.GetAsync(u => u.SmsVerificationToken == request.SmsVerificationToken, cancellationToken);

            if (!user.IsConfirmed)
            {
                return this.mapper.Map<UserDto>(user);
            }

            if (user != null && user.SmsTokenExpiration > DateTime.UtcNow)
            {
                user.SmsVerificationToken = null;
                user.SmsTokenExpiration = null;

                await this.usersRepository.UpdateUserAsync(user);

                return this.mapper.Map<UserDto>(user);
            }

            return null;
        }
    }
}
