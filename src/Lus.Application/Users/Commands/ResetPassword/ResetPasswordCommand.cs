using MediatR;

namespace Lus.Application.Users.Commands.ResetPassword
{
    public record ResetPasswordCommand(string Password, string Email) : IRequest<Unit>;
}
