using AutoMapper;
using EasyCaching.Core;
using MediatR;
using Lus.Application.Organizations.Repositories;
using Lus.Authorization;
using Lus.Contracts.Organizations;

namespace Lus.Application.Organizations.Queries.GetCurrentOrganization
{
    public class GetCurrentOrganizationQueryHandler : IRequestHandler<GetCurrentOrganizationQuery, OrganizationDto>
    {
        private readonly IOrganizationsRepository organizationsRepository;
        private readonly IMapper mapper;
        private readonly IEasyCachingProvider provider;
        private readonly IUserAccessor userAccessor;

        public GetCurrentOrganizationQueryHandler(IOrganizationsRepository organizationsRepository, IMapper mapper, IEasyCachingProvider provider, IUserAccessor userAccessor)
        {
            this.userAccessor = userAccessor;
            this.organizationsRepository = organizationsRepository;
            this.mapper = mapper;
            this.provider = provider;
        }

        public async Task<OrganizationDto> Handle(GetCurrentOrganizationQuery request,
            CancellationToken cancellationToken)
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

            var organization = await this.organizationsRepository.GetWithIncludeAsync(organizationId.Value, cancellationToken);

            if (organization == null)
            {
                return null;
            }

            var organizationDto = this.mapper.Map<OrganizationDto>(organization);

            return organizationDto;
        }
    }
}
