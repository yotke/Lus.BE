using MediatR;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Queries.GetUserInfo
{
    public record GetUserInfoQuery() : IRequest<UserInfoDto>;
}
