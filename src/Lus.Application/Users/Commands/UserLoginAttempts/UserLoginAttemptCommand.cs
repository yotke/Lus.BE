using MediatR;
using Lus.Contracts.Users.Types;

namespace Lus.Application.Users.Commands.UserLoginAttempts
{
    public record UserLoginAttemptCommand : IRequest<Unit>
    {
        public string UserName { get; init; }

        public UserLoginAttemptType UserLoginAttemptType { get; init; }

        public LoginFailReasonType LoginFailReason { get; init; }

        public int? UserId { get; set; }
    }
}
