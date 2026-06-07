using MediatR;
using Lus.Contracts.Users.Types;

namespace Lus.Application.Users.Commands.DeleteUserLoginAttemptsByType
{
    public record DeleteUserLoginAttemptsByTypeCommand(int UserId, LoginFailReasonType LoginFailReasonType) : IRequest<Unit>;
}
