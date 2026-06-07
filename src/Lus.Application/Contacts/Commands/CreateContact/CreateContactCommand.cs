using MediatR;
using Lus.Contracts.Contacts;

namespace Lus.Application.Contacts.Commands.CreateContact
{
    public record CreateContactCommand : IRequest<ContactDto>
    {
        public string IdNumber { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public int? OrganizationId { get; set; }
    }
}
