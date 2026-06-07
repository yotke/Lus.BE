using MediatR;
using Lus.Contracts.Contacts;

namespace Lus.Application.Organizations.Queries.GetOrganizationContacts
{
    public record GetOrganizationContactsQuery : IRequest<ICollection<ContactDto>>;
}
