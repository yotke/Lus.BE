using MediatR;

namespace Lus.Application.Users.Commands.CreateUser
{
    public record CreateUserCommand : IRequest<Unit>
    {
        public string Email { get; init; }

        public string FirstName { get; init; }

        public string LastName { get; init; }

        public string Password { get; init; }

        public string Phone { get; init; }

        public string IdNumber { get; init; }
    }
}
