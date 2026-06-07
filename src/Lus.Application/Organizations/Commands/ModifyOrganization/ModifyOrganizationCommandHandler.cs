using AutoMapper;
using MediatR;
using Lus.Application.Common.Extensions;
using Lus.Application.Organizations.Entities;
using Lus.Application.Organizations.Repositories;
using Lus.Application.Contacts.Entities;
using Lus.Application.Contacts.Repositories;
using Lus.Contracts.Organizations;

namespace Lus.Application.Organizations.Commands.ModifyOrganization
{
    public class ModifyOrganizationCommandHandler : IRequestHandler<ModifyOrganizationCommand, OrganizationDto>
    {
        private readonly IOrganizationsRepository OrganizationsRepository;
        private readonly IContactsRepository contactsRepository;
        private readonly IMapper mapper;
        private readonly List<string> OrganizationListOfPropertiesToIgnore = new List<string> { "Contacts" };
        private readonly List<string> ContactListOfPropertiesToIgnore = new List<string> { "OrganizationId", "Organization" };

        public ModifyOrganizationCommandHandler(IOrganizationsRepository OrganizationsRepository, IContactsRepository contactsRepository, IMapper mapper)
        {
            this.OrganizationsRepository = OrganizationsRepository;
            this.contactsRepository = contactsRepository;
            this.mapper = mapper;
        }

        public async Task<OrganizationDto> Handle(ModifyOrganizationCommand modifyCommand, CancellationToken cancellationToken)
        {
            var savedOrganization = await this.OrganizationsRepository.GetSingleEntityAsync(modifyCommand.Id, cancellationToken);

            savedOrganization.CopyIfDifferent(modifyCommand, OrganizationListOfPropertiesToIgnore);

            savedOrganization = await this.OrganizationsRepository.UpdateAsync(savedOrganization, cancellationToken);
            foreach (var contact in modifyCommand.Contacts)
            {
                Contact item;
                if (contact.Id > 0)
                {
                    var savedContact = await this.contactsRepository.GetSingleEntityAsync(contact.Id, cancellationToken);

                    savedContact.CopyIfDifferent(contact, ContactListOfPropertiesToIgnore);

                    item = await this.contactsRepository.UpdateAsync(savedContact, cancellationToken);
                }
                else
                {
                    ; item = await this.contactsRepository.AddAsync(this.mapper.Map<Contact>(contact), cancellationToken);
                }
            }

            return this.mapper.Map<OrganizationDto>(savedOrganization);
        }
    }
}
