using MediatR;
using Lus.Contracts.Contacts;

namespace Lus.Application.Contacts.Queries
{
    public record GetContactsQuery(int organizationId) : IRequest<ICollection<ContactDto>>;
}
