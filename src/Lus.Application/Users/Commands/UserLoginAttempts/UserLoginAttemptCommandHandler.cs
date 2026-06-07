using AutoMapper;
using MediatR;
using Lus.Application.Users.Entities;
using Lus.Application.Users.Repositories;
using Lus.Contracts.Users.Types;

namespace Lus.Application.Users.Commands.UserLoginAttempts
{
    public class UserLoginAttemptCommandHandler : IRequestHandler<UserLoginAttemptCommand, Unit>
    {
        private readonly IUserLoginAttemptsRepository userLoginAttemptRepository;
        private readonly IUsersRepository usersRepository;
        private readonly IMapper mapper;

        public UserLoginAttemptCommandHandler(IUsersRepository usersRepository, IUserLoginAttemptsRepository userLoginAttemptRepository, IMapper mapper)
        {
            this.usersRepository = usersRepository;
            this.userLoginAttemptRepository = userLoginAttemptRepository;
            this.mapper = mapper;
        }

        public async Task<Unit> Handle(UserLoginAttemptCommand command, CancellationToken cancellationToken)
        {
            if (command.UserLoginAttemptType == UserLoginAttemptType.Succeed)
            {
                var user = await this.usersRepository.GetWithIncludeAsync(command.UserId.Value, cancellationToken, u => u.UserLoginAttempts);
                await this.userLoginAttemptRepository.DeleteAllAsync(user.UserLoginAttempts, cancellationToken);
            }
            else
            {
                await this.userLoginAttemptRepository.AddAsync(this.mapper.Map<UserLoginAttempt>(command), cancellationToken);
            }

            return Unit.Value;
        }
    }
}
