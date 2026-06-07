using MediatR;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Commands.LoginUserByToken
{
    public record LoginUserByTokenCommand(string SmsVerificationToken) : IRequest<UserDto>;
}
