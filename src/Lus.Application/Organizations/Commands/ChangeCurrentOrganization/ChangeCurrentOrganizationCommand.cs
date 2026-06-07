using MediatR;

namespace Lus.Application.Organizations.Commands.ChangeCurrentOrganization
{
    public record ChangeCurrentOrganizationCommand(int OrganizationId) : IRequest<Unit>;
}
