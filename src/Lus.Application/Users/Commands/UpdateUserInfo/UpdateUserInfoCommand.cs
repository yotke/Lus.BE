using MediatR;

namespace Lus.Application.Users.Commands.UpdateUserInfo
{
    public record UpdateUserInfoCommand : IRequest<Unit>
    {
        public string FirstName { get; init; }

        public string LastName { get; init; }

        public string Phone { get; init; }

        public string IdNumber { get; init; }
    }
}
