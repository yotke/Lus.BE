using AutoMapper;
using MediatR;
using Lus.Application.Common.Extensions;
using Lus.Application.Organizations.Repositories;
using Lus.Contracts.Organizations;

namespace Lus.Application.Organizations.Queries.GetOrganization
{
    public class GetOrganizationQueryHandler : IRequestHandler<GetOrganizationQuery, OrganizationDto>
    {
        private readonly IOrganizationsRepository organizationsRepository;
        private readonly IMapper mapper;

        public GetOrganizationQueryHandler(IOrganizationsRepository organizationsRepository, IMapper mapper)
        {
            this.organizationsRepository = organizationsRepository;
            this.mapper = mapper;
        }

        public async Task<OrganizationDto> Handle(GetOrganizationQuery request, CancellationToken cancellationToken)
        {
            var organization = await this.organizationsRepository.GetWithIncludeAsync(request.Id, cancellationToken,org=>org.Contacts);
                //,org => org.City, org => org.GeoRegion);

            var organizationDto = this.mapper.Map<OrganizationDto>(organization);

            return organizationDto;
        }
    }
}
