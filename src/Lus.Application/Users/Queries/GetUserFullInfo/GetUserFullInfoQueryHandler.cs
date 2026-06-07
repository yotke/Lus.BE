using AutoMapper;
using MediatR;
using Lus.Application.Users.Repositories;
using Lus.Authorization;
using Lus.Contracts.Organizations;
using Lus.Contracts.Roles;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Queries.GetUserFullInfo
{
    public class GetUserFullInfoQueryHandler : IRequestHandler<GetUserFullInfoQuery, UserFullInfoDto>
    {
        private readonly IUsersRepository usersRepository;
        private readonly IMapper mapper;
        private readonly IUserAccessor userAccessor;
        private readonly IUserRolesRepository userRolesRepository;
        private readonly IUserOrganizationsRepository userOrganizationsRepository;

        public GetUserFullInfoQueryHandler(IUserOrganizationsRepository userOrganizationsRepository, IUserRolesRepository userRolesRepository, IUserAccessor userAccessor, IUsersRepository usersRepository, IMapper mapper)
        {
            this.userRolesRepository = userRolesRepository;
            this.userOrganizationsRepository = userOrganizationsRepository;
            this.userAccessor = userAccessor;
            this.usersRepository = usersRepository;
            this.mapper = mapper;
        }

        public async Task<UserFullInfoDto> Handle(GetUserFullInfoQuery request, CancellationToken cancellationToken)
        {
            var user = await this.usersRepository.GetWithIncludeAsync(request.UserId, cancellationToken, u => u.UserRoles, u => u.UserOrganizations);

            var userDto = this.mapper.Map<UserFullInfoDto>(user);
            if (user?.UserRoles.Any() ?? false)
            {
                var roles = await this.userRolesRepository.GetAllListAsync(ur => ur.UserId == request.UserId,
                    cancellationToken, ur => ur.Role);

                userDto.Roles = this.mapper.Map<ICollection<RoleDto>>(roles.Select(ur => ur.Role).ToList());
            }

            if (user?.UserOrganizations.Any() ?? false)
            {
                var organizations = await this.userOrganizationsRepository.GetAllListAsync(ur => ur.UserId == request.UserId,
                    cancellationToken, ur => ur.Organization);

                userDto.Organizations = this.mapper.Map<ICollection<OrganizationDto>>(organizations.Select(ur => ur.Organization).ToList());
            }

            return userDto;
        }
    }
}
