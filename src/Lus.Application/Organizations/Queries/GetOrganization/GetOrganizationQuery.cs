using MediatR;
using Lus.Contracts.Organizations;

namespace Lus.Application.Organizations.Queries.GetOrganization
{
    public record GetOrganizationQuery(int Id) : IRequest<OrganizationDto>;
}
