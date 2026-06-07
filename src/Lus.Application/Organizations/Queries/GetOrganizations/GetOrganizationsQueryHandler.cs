using AutoMapper;
using MediatR;
using Lus.Application.Organizations.Repositories;
using Lus.Contracts.Organizations;
using System.Linq;

namespace Lus.Application.Organizations.Queries.GetOrganizations
{
    public class GetOrganizationsQueryHandler : IRequestHandler<GetOrganizationsQuery, ICollection<OrganizationInfoDto>>
    {
        private readonly IOrganizationsRepository organizationsRepository;
        private readonly IMapper mapper;

        public GetOrganizationsQueryHandler(IOrganizationsRepository organizationsRepository, IMapper mapper)
        {
            this.organizationsRepository = organizationsRepository;
            this.mapper = mapper;
        }

        public async Task<ICollection<OrganizationInfoDto>> Handle(GetOrganizationsQuery request,
            CancellationToken cancellationToken)
        {
            //var organizations = await this.organizationsRepository.GetAllListAsync(_ => true, cancellationToken, org => org.City, org => org.GeoRegion);
            var organizations = await this.organizationsRepository.GetAllListAsync(o =>
              (string.IsNullOrWhiteSpace(request.OrganizationId) || o.Id.ToString().Contains(request.OrganizationId)) &&
              (string.IsNullOrWhiteSpace(request.OrganizationName) || o.Name.Contains(request.OrganizationName)) &&
              (string.IsNullOrWhiteSpace(request.AccountingNumber) || o.AccountingNumber.ToString().Contains(request.AccountingNumber)) &&
              (request.Active.HasValue ? o.Active == request.Active.Value : true)
              //(string.IsNullOrWhiteSpace(request.LastName) || u.LastName.Contains(request.LastName)) &&
              //(string.IsNullOrWhiteSpace(request.IdNumber) || u.IdNumber.Contains(request.IdNumber))
              , cancellationToken );
            var organizationsDto = this.mapper.Map<ICollection<OrganizationInfoDto>>(organizations);

            return organizationsDto;
        }
    }
}
