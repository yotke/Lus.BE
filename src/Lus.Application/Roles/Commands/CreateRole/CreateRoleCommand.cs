using MediatR;

namespace Lus.Application.Roles.Commands.CreateRole
{
    public record CreateRoleCommand : IRequest<Unit>
    {
        public string Name { get; init; }

        public int Immunity { get; init; }

        public string HebrewName { get; init; }

        public bool? ShowToAdmin { get; init; }

        public int? OrganizationId { get; init; }
    }
}
