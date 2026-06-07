using MediatR;
using Lus.Application.Common.Extensions;
using Lus.Application.Organizations.Repositories;
using Lus.Application.Users.Entities;
using Lus.Application.Users.Queries.GetUserFullInfo;
using Lus.Application.Users.Repositories;
using Lus.Authorization;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Commands.AddOrganizationToUser
{
    public class AddOrganizationToUserCommandHandler : IRequestHandler<AddOrganizationToUserCommand, UserFullInfoDto>
    {
        private readonly IOrganizationsRepository organizationsRepository;
        private readonly IUsersRepository usersRepository;
        private readonly IUserOrganizationsRepository userOrganizationRepository;
        private readonly IUserRolesRepository userRolesRepository;
        private readonly IMediator mediator;
        private readonly IUserAccessor userAccessor;

        public AddOrganizationToUserCommandHandler(IUserRolesRepository userRolesRepository, IUsersRepository usersRepository, IOrganizationsRepository organizationsRepository, IUserOrganizationsRepository userOrganizationRepository, IMediator mediator, IUserAccessor userAccessor)
        {
            this.userRolesRepository = userRolesRepository;
            this.usersRepository = usersRepository;
            this.organizationsRepository = organizationsRepository;
            this.userAccessor = userAccessor;
            this.userOrganizationRepository = userOrganizationRepository;
            this.mediator = mediator;
        }

        public async Task<UserFullInfoDto> Handle(AddOrganizationToUserCommand command, CancellationToken cancellationToken)
        {
            if (command.UserId.HasValue)
            {
                await this.usersRepository.GetSingleEntityAsync(command.UserId.Value, cancellationToken);
            }

            var userId = command.UserId ?? this.userAccessor.ProjectUser.Id;

            var userOrganizations = await this.userOrganizationRepository.GetAllListAsync(uo => uo.UserId == userId && command.OrganizationsId.All(orgId => orgId != uo.OrganizationId), cancellationToken);

            foreach (var userOrganization in userOrganizations)
            {
                await this.userOrganizationRepository.DeleteAsync(userOrganization, cancellationToken);
            }

            var userRoles = await this.userRolesRepository.GetAllListAsync(ur => ur.UserId == userId, cancellationToken, ur => ur.Role);
            foreach (var userRole in userRoles.Where(ur => command.OrganizationsId.All(orgId => orgId != ur.Role.OrganizationId)))
            {
                await this.userRolesRepository.DeleteAsync(userRole, cancellationToken);
            }

            command.OrganizationsId.ToList().ForEach(async organizationId =>
            {
                await this.organizationsRepository.GetSingleEntityAsync(organizationId, cancellationToken);

                var userOrganization = await this.userOrganizationRepository.GetAsync(ur => ur.OrganizationId == organizationId && ur.UserId == userId, cancellationToken);
                if (userOrganization == null)
                {
                    await this.userOrganizationRepository.AddAsync(new UserOrganization { OrganizationId = organizationId, UserId = userId }, cancellationToken);
                }
            });


            return await this.mediator.Send(new GetUserFullInfoQuery(userId), cancellationToken);
        }
    }
}
