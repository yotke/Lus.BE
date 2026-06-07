using AutoMapper;
using EasyCaching.Core;
using MediatR;
using Microsoft.AspNetCore.Http;
using Lus.Application.Users.Commands.AddUserLoginInfo;
using Lus.Application.Users.Entities;
using Lus.Application.Users.Repositories;
using Lus.Contracts.Organizations;
using Lus.Contracts.Roles;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Queries.GetAuthUserInfo
{
    public class GetAuthUserInfoQueryHandler : IRequestHandler<GetAuthUserInfoQuery, AuthUserInfo>
    {
        private readonly IUsersRepository usersRepository;
        private readonly IMediator mediator;
        private readonly IMapper mapper;
        private readonly IUserRolesRepository userRolesRepository;
        private readonly IUserOrganizationsRepository userOrganizationsRepository;
        private readonly IEasyCachingProvider provider;

        public GetAuthUserInfoQueryHandler(IMediator mediator, IEasyCachingProvider provider, IUserOrganizationsRepository userOrganizationsRepository, IUsersRepository usersRepository, IMapper mapper, IUserRolesRepository userRolesRepository)
        {
            this.mediator = mediator;
            this.userOrganizationsRepository = userOrganizationsRepository;
            this.provider = provider;
            this.usersRepository = usersRepository;
            this.userRolesRepository = userRolesRepository;
            this.mapper = mapper;
        }

        public async Task<AuthUserInfo> Handle(GetAuthUserInfoQuery request, CancellationToken cancellationToken)
        {
            var userInfo = await TryGetByEmailAsync(request.Email, cancellationToken) ??
                           await TryGetByIdAsync(request.UserId, cancellationToken);

            var userInfoDto = this.mapper.Map<AuthUserInfo>(userInfo);

            if (userInfo?.UserRoles.Any() ?? false)
            {
                var roles = await this.userRolesRepository.GetAllListAsync(ur => ur.UserId == userInfoDto.Id,
                    cancellationToken, ur => ur.Role);

                userInfoDto.Roles = roles.Select(ur => new AuthRoleDto { Name = ur.Role.Name, Immunity = ur.Role.Immunity, OrganizationId = ur.Role.OrganizationId }).ToList();

                await UpdateClaimsAsync(userInfo, userInfoDto.Roles);
            }

            if (userInfo?.UserOrganizations.Any() ?? false)
            {
                var organizations = await this.userOrganizationsRepository.GetAllListAsync(ur => ur.UserId == userInfoDto.Id,
                    cancellationToken, ur => ur.Organization);

                userInfoDto.Organizations = organizations.Select(ur => new AuthOrganizationDto { Name = ur.Organization.Name, Id = ur.OrganizationId, AccountingNumber = ur.Organization.AccountingNumber }).ToList();

                await this.provider.SetAsync<int>(
                    $"{ApplicationConstants.CachedProviderKeys.UserOrganizationCacheKey}{userInfo.Id}", userInfoDto.Organizations.First().Id,
                    TimeSpan.FromDays(365), cancellationToken);
            }

            if (userInfo != null)
            {
                await this.mediator.Send(new AddUserLoginInfoCommand(this.mapper.Map<UserDto>(userInfo)), cancellationToken);
            }

            return userInfoDto;
        }

        private async Task UpdateClaimsAsync(User updatedUser, ICollection<AuthRoleDto> roles)
        {
            var claims = (updatedUser.Claims ?? Enumerable.Empty<KeyValuePair<string, string>>()).ToList();


            var claimsToUpdate = claims.Where(cl => !string.Equals(cl.Key, ApplicationConstants.ClaimsTypes.UserRole,
                StringComparison.CurrentCultureIgnoreCase)).ToList();

            foreach (var role in roles)
            {
                claimsToUpdate.Add(new(ApplicationConstants.ClaimsTypes.UserRole,
                    $"{role.OrganizationId}{role.Name}" ?? ApplicationConstants.ClaimsValues.NoRole));
            }

            updatedUser.Claims = claimsToUpdate;
            await this.usersRepository.UpdateUserClaimsAsync(updatedUser.Id, claimsToUpdate);
        }

        private async Task<User> TryGetByIdAsync(int? userId, CancellationToken cancellationToken)
        {
            if (!userId.HasValue)
            {
                return null;
            }

            return await this.usersRepository.GetWithIncludeAsync(userId.Value, cancellationToken, u => u.UserRoles, u => u.UserOrganizations);
        }

        private async Task<User> TryGetByEmailAsync(string email, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(email))
            {
                return null;
            }

            return await this.usersRepository.GetWithIncludeAsync(u => u.UserName == email, cancellationToken, u => u.UserRoles, u => u.UserOrganizations);
        }
    }
}
