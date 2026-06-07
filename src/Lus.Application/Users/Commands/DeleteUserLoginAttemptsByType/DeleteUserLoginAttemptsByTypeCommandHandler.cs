using AutoMapper;
using MediatR;
using Lus.Application.Users.Repositories;
using Lus.Contracts.Users.Types;

namespace Lus.Application.Users.Commands.DeleteUserLoginAttemptsByType
{
    public class DeleteUserLoginAttemptsByTypeCommandHandler : IRequestHandler<DeleteUserLoginAttemptsByTypeCommand, Unit>
    {
        private readonly IUserLoginAttemptsRepository userLoginAttemptRepository;
        private readonly IUsersRepository usersRepository;

        public DeleteUserLoginAttemptsByTypeCommandHandler(IUsersRepository usersRepository, IUserLoginAttemptsRepository userLoginAttemptRepository)
        {
            this.usersRepository = usersRepository;
            this.userLoginAttemptRepository = userLoginAttemptRepository;
        }

        public async Task<Unit> Handle(DeleteUserLoginAttemptsByTypeCommand command, CancellationToken cancellationToken)
        {
            var user = await this.usersRepository.GetWithIncludeAsync(command.UserId, cancellationToken, u => u.UserLoginAttempts);

            await this.userLoginAttemptRepository.DeleteAllAsync(user.UserLoginAttempts.Where(ua => ua.LoginFailReason == LoginFailReasonType.WrongPassword), cancellationToken);

            return Unit.Value;
        }
    }
}
