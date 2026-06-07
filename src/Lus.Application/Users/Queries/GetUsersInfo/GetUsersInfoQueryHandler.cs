using AutoMapper;
using EasyCaching.Core;
using MediatR;
using Lus.Application.Roles.Repositories;
using Lus.Application.Users.Repositories;
using Lus.Authorization;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Queries.GetUsersInfo
{
    public class GetUsersInfoQueryHandler : IRequestHandler<GetUsersInfoQuery, ICollection<UserInfoDto>>
    {
        private readonly IUsersRepository usersRepository;
        private readonly IRolesRepository rolesRepository;
        private readonly IMapper mapper;
        private readonly IUserAccessor userAccessor;
        private readonly IEasyCachingProvider provider;
        private readonly string DEVELOPER_ROLE = "DEVELOPER";
            
        public GetUsersInfoQueryHandler(IEasyCachingProvider provider, IUserAccessor userAccessor, IRolesRepository rolesRepository, IUsersRepository usersRepository, IMapper mapper)
        {
            this.provider = provider;
            this.userAccessor = userAccessor;
            this.usersRepository = usersRepository;
            this.rolesRepository = rolesRepository;
            this.mapper = mapper;
        }

        public async Task<ICollection<UserInfoDto>> Handle(GetUsersInfoQuery request, CancellationToken cancellationToken)
        {
            var organizationId = await this.provider.GetAsync<int>(
                $"{ApplicationConstants.CachedProviderKeys.UserOrganizationCacheKey}{this.userAccessor.ProjectUser.Id}", cancellationToken);

            var roles = this.userAccessor.ProjectUser.Roles;
            var orgId = organizationId.HasValue ? organizationId.Value : 0;
            var developerRoles =await this.rolesRepository.GetAllListAsync(r => r.Immunity > 90);
            var users = await this.usersRepository.GetAllListAsync(u =>
                (string.IsNullOrWhiteSpace(request.Phone) || u.Phone.Contains(request.Phone)) &&
                (string.IsNullOrWhiteSpace(request.Email) || u.Email.Contains(request.Email)) &&
                (string.IsNullOrWhiteSpace(request.FirstName) || u.FirstName.Contains(request.FirstName)) &&
                (string.IsNullOrWhiteSpace(request.LastName) || u.LastName.Contains(request.LastName)) &&
                (string.IsNullOrWhiteSpace(request.IdNumber) || u.IdNumber.Contains(request.IdNumber)) &&
                (roles.Contains(DEVELOPER_ROLE) || u.UserOrganizations.Any(uo => uo.OrganizationId == orgId ))
                , cancellationToken, u => u.UserOrganizations,u=>u.UserRoles);

            users = users.Where(u=>roles.Contains(DEVELOPER_ROLE) || ((u.UserRoles != null && u.UserRoles.Count>0) ? u.UserRoles.Any(ur => developerRoles.Any(dr => dr.Id == ur.RoleId)) != true : true));
            var usersDto = this.mapper.Map<ICollection<UserInfoDto>>(users);

            return usersDto;
        }
    }
}
