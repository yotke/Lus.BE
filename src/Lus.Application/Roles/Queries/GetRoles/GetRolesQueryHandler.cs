using AutoMapper;
using EasyCaching.Core;
using MediatR;
using Lus.Application.Roles.Entities;
using Lus.Application.Roles.Repositories;
using Lus.Authorization;
using Lus.Contracts.Roles;

namespace Lus.Application.Roles.Queries.GetRoles
{
    public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, ICollection<RoleDto>>
    {
        private readonly IRolesRepository rolesRepository;
        private readonly IMapper mapper;
        private readonly IEasyCachingProvider provider;
        private readonly IUserAccessor userAccessor;

        public GetRolesQueryHandler(IRolesRepository rolesRepository, IMapper mapper, IEasyCachingProvider provider, IUserAccessor userAccessor)
        {
            this.userAccessor = userAccessor;
            this.provider = provider;
            this.rolesRepository = rolesRepository;
            this.mapper = mapper;
        }

        public async Task<ICollection<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
        {
            var organizationId = await this.provider.GetAsync<int>(
                $"{ApplicationConstants.CachedProviderKeys.UserOrganizationCacheKey}{this.userAccessor.ProjectUser.Id}", cancellationToken);

            IEnumerable<Role> roles;
            if (request.IgnoreOrganization)
            {
                roles = await this.rolesRepository.GetAllListAsync(r => r.OrganizationId == null, cancellationToken);
            }
            else
            {
                roles = await this.rolesRepository.GetAllListAsync(r => r.OrganizationId == null || (!organizationId.HasValue || r.OrganizationId == organizationId.Value), cancellationToken);
            }


            var rolesDto = this.mapper.Map<ICollection<RoleDto>>(roles);

            return rolesDto.GroupBy(r => r.Name)
                .SelectMany(g => g.Count() > 1 ? g.Where(i => i.OrganizationId.HasValue) : g)
                .ToList();
        }
    }
}
