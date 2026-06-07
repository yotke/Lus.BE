using AutoMapper;
using EasyCaching.Core;
using MediatR;
using Lus.Application.Contacts.Repositories;
using Lus.Authorization;
using Lus.Contracts.Contacts;

namespace Lus.Application.Organizations.Queries.GetOrganizationContacts
{
    public class GetOrganizationContactsQueryHandler : IRequestHandler<GetOrganizationContactsQuery, ICollection<ContactDto>>
    {
        private readonly IContactsRepository contactsRepository;
        private readonly IMapper mapper;
        private readonly IEasyCachingProvider provider;
        private readonly IUserAccessor userAccessor;

        public GetOrganizationContactsQueryHandler(IContactsRepository contactsRepository, IMapper mapper, IEasyCachingProvider provider, IUserAccessor userAccessor)
        {
            this.userAccessor = userAccessor;
            this.contactsRepository = contactsRepository;
            this.mapper = mapper;
            this.provider = provider;
        }

        public async Task<ICollection<ContactDto>> Handle(GetOrganizationContactsQuery request, CancellationToken cancellationToken)
        {

            if (!await this.provider.ExistsAsync($"{ApplicationConstants.CachedProviderKeys.UserOrganizationCacheKey}{this.userAccessor.ProjectUser.Id}", cancellationToken))
            {
                return null;
            }

            var organizationId = await this.provider.GetAsync<int>(
                 $"{ApplicationConstants.CachedProviderKeys.UserOrganizationCacheKey}{this.userAccessor.ProjectUser.Id}", cancellationToken);

            if (!organizationId.HasValue)
            {
                return null;
            }

            var contacts = await this.contactsRepository.GetAllListAsync(c => c.OrganizationId == organizationId.Value, cancellationToken);

            var contactsDto = this.mapper.Map<ICollection<ContactDto>>(contacts);

            return contactsDto;
        }
    }
}
