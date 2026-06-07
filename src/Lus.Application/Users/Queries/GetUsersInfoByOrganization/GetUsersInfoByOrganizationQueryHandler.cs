using AutoMapper;
using EasyCaching.Core;
using MediatR;
using Lus.Application.Roles.Repositories;
using Lus.Application.Users.Repositories;
using Lus.Authorization;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Queries.GetUsersInfoByOrganization
{
    public class GetUsersInfoByOrganizationQueryHandler : IRequestHandler<GetUsersInfoByOrganizationQuery, ICollection<UserInfoDto>>
    {
        private readonly IUsersRepository usersRepository;
        private readonly IRolesRepository rolesRepository;
        private readonly IMapper mapper;
        private readonly IUserAccessor userAccessor;
        private readonly IEasyCachingProvider provider;

        public GetUsersInfoByOrganizationQueryHandler(IRolesRepository rolesRepository, IEasyCachingProvider provider, IUserAccessor userAccessor, IUsersRepository usersRepository, IMapper mapper)
        {
            this.rolesRepository = rolesRepository;
            this.provider = provider;
            this.userAccessor = userAccessor;
            this.usersRepository = usersRepository;
            this.mapper = mapper;
        }

        public async Task<ICollection<UserInfoDto>> Handle(GetUsersInfoByOrganizationQuery request, CancellationToken cancellationToken)
        {
            // TODO ---> Create a async get of all users by OrganizaitonId and by User Role 
            // If user Role is less then 95 the handleFunction should only return the
            // organization user of the current user organization/s (get the organization and roles from userAccessor)

            var organizationId = await this.provider.GetAsync<int>(
                $"{ApplicationConstants.CachedProviderKeys.UserOrganizationCacheKey}{this.userAccessor.ProjectUser.Id}", cancellationToken);

            var orgId = organizationId.HasValue ? organizationId.Value : 0;

            var users = await this.usersRepository.GetAllListAsync(u =>
                    u.IsConfirmed &&
                        ((!string.IsNullOrWhiteSpace(request.Phone) && u.Phone.Contains(request.Phone)) ||
                         (!string.IsNullOrWhiteSpace(request.Email) && u.Email.Contains(request.Email)) ||
                         (!string.IsNullOrWhiteSpace(request.FirstName) && u.FirstName.Contains(request.FirstName)) ||
                         (!string.IsNullOrWhiteSpace(request.LastName) && u.LastName.Contains(request.LastName)) ||
                         (!string.IsNullOrWhiteSpace(request.IdNumber) && u.IdNumber.Contains(request.IdNumber)))
                , cancellationToken, u => u.UserOrganizations, u => u.UserRoles);

            var rolesByOrganization =
                await this.rolesRepository.GetAllListAsync(r => r.OrganizationId == orgId && r.Name == "COMMITTEE_MEMBER", cancellationToken);

            var usersDto = this.mapper.Map<ICollection<UserInfoDto>>(users.Where(u =>
                (u.UserOrganizations?.Any(uo => uo.OrganizationId == orgId) ?? false) &&
                (u.UserRoles?.Any(ur => rolesByOrganization?.Any(r => ur.RoleId == r.Id) ?? false) ?? false)
                ));
            
            return usersDto;
        }
    }
}
