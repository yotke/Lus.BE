using MediatR;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Queries.GetUserFullInfo
{
    public record GetUserFullInfoQuery(int UserId) : IRequest<UserFullInfoDto>;
}
