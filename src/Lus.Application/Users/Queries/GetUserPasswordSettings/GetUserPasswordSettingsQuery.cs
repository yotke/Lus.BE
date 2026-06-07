using MediatR;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Queries.GetUserPasswordSettings
{
    public record GetUserPasswordSettingsQuery() : IRequest<UserPasswordSettingsDto>;
}
