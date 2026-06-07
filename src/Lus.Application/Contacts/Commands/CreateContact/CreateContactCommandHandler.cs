using AutoMapper;
using MediatR;
using Lus.Application.Contacts.Entities;
using Lus.Application.Contacts.Repositories;
using Lus.Contracts.Contacts;

namespace Lus.Application.Contacts.Commands.CreateContact
{
    public class CreateContactCommandHandler : IRequestHandler<CreateContactCommand, ContactDto>
    {
        private readonly IContactsRepository contactsRepository;
        private readonly IMapper mapper;

        public CreateContactCommandHandler(IContactsRepository contactsRepository, IMapper mapper)
        {
            this.contactsRepository = contactsRepository;
            this.mapper = mapper;
        }

        public async Task<ContactDto> Handle(CreateContactCommand createCommand, CancellationToken cancellationToken)
        {
            var contact = this.mapper.Map<Contact>(createCommand);

            contact = await this.contactsRepository.AddAsync(contact, cancellationToken);

            return this.mapper.Map<ContactDto>(contact);
        }
    }
}
