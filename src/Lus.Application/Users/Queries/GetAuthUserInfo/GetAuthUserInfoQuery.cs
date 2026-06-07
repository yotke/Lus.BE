using MediatR;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Queries.GetAuthUserInfo
{
    public record GetAuthUserInfoQuery : IRequest<AuthUserInfo>
    {
        public GetAuthUserInfoQuery(string email) => Email = email;

        public GetAuthUserInfoQuery(int userId) => UserId = userId;

        public int? UserId { get; init; }

        public string Email { get; init; }
    }
}
