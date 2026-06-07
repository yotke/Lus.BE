using MediatR;
using Lus.Contracts.Users.Types;

namespace Lus.Application.Users.Commands.UserResetPassword
{
    public record UserResetPasswordCommand(string Email, ResetPasswordType ResetPasswordType) : IRequest<Unit>;
}
