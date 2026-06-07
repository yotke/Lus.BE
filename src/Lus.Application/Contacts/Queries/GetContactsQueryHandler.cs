using AutoMapper;
using MediatR;
using Lus.Application.Contacts.Repositories;
using Lus.Contracts.Contacts;

namespace Lus.Application.Contacts.Queries
{
    public class GetContactsQueryHandler : IRequestHandler<GetContactsQuery, ICollection<ContactDto>>
    {
        private readonly IContactsRepository contactsRepository;
        private readonly IMapper mapper;

        public GetContactsQueryHandler(IContactsRepository contactsRepository, IMapper mapper)
        {
            this.contactsRepository = contactsRepository;
            this.mapper = mapper;
        }

        public async Task<ICollection<ContactDto>> Handle(GetContactsQuery request,
            CancellationToken cancellationToken)
        {
            var contacts = await this.contactsRepository.GetAllListAsync(c=>c.OrganizationId==request.organizationId, cancellationToken);

            var contactsDto = this.mapper.Map<ICollection<ContactDto>>(contacts);

            return contactsDto;
        }
    }
}
