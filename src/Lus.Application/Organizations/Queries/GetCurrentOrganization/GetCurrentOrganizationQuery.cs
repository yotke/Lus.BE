using MediatR;
using Lus.Contracts.Organizations;

namespace Lus.Application.Organizations.Queries.GetCurrentOrganization
{
    public record GetCurrentOrganizationQuery : IRequest<OrganizationDto>;
}
