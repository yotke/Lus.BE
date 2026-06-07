using MediatR;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Queries.GetUserData
{
    public record GetUserDataQuery(int UserId) : IRequest<UserDataDto>;
}
