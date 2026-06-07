using AutoMapper;
using MediatR;
using Lus.Application.Common.Extensions;
using Lus.Application.Organizations.Repositories;
using Lus.Application.Roles.Entities;
using Lus.Application.Roles.Repositories;
using Lus.Application.Users.Entities;
using Lus.Application.Users.Queries.GetUserFullInfo;
using Lus.Application.Users.Repositories;
using Lus.Authorization;
using Lus.Contracts.Users;

namespace Lus.Application.Users.Commands.AddRoleToUser
{
    public class AddRoleToUserCommandHandler : IRequestHandler<AddRoleToUserCommand, UserFullInfoDto>
    {
        private readonly IRolesRepository rolesRepository;
        private readonly IUsersRepository usersRepository;
        private readonly IOrganizationsRepository organizationsRepository;
        private readonly IUserRolesRepository userRolesRepository;
        private readonly IMediator mediator;
        private readonly IUserAccessor userAccessor;

        public AddRoleToUserCommandHandler(IUsersRepository usersRepository, IOrganizationsRepository organizationsRepository, IRolesRepository rolesRepository, IUserRolesRepository userRolesRepository, IMediator mediator, IUserAccessor userAccessor)
        {
            this.organizationsRepository = organizationsRepository;
            this.usersRepository = usersRepository;
            this.rolesRepository = rolesRepository;
            this.userAccessor = userAccessor;
            this.userRolesRepository = userRolesRepository;
            this.mediator = mediator;
        }

        public async Task<UserFullInfoDto> Handle(AddRoleToUserCommand command, CancellationToken cancellationToken)
        {
            var roleToDelete = await this.userRolesRepository.GetAsync(ur => ur.UserId == command.UserId && ur.RoleId == 3, cancellationToken);
            if (roleToDelete != null)
            {
                await this.userRolesRepository.DeleteAsync(roleToDelete, cancellationToken);
            }

            if (command.UserId.HasValue)
            {
                await this.usersRepository.GetSingleEntityAsync(command.UserId.Value, cancellationToken);
            }

            var userId = command.UserId ?? this.userAccessor.ProjectUser.Id;

            await this.organizationsRepository.GetSingleEntityAsync(command.OrganizationId, cancellationToken);

            var organizationRoles = await this.rolesRepository.GetAllListAsync(r => r.OrganizationId == command.OrganizationId
            && r.UserRoles.Any(ur => ur.UserId == userId), cancellationToken, r => r.UserRoles);
            organizationRoles.ToList().ForEach(async role =>
            {
                var savedRoleToDelete = await this.userRolesRepository.GetAsync(ur => ur.UserId == command.UserId && ur.RoleId == role.Id, cancellationToken);
                if (savedRoleToDelete != null)
                {
                    await this.userRolesRepository.DeleteAsync(savedRoleToDelete, cancellationToken);
                }
            });

            command.RolesId.ToList().ForEach(async RoleId =>
            {
                var role = await this.rolesRepository.GetSingleEntityAsync(RoleId, cancellationToken);

                var roleToAdd = await this.rolesRepository.GetAsync(r => r.Name == role.Name && r.OrganizationId == command.OrganizationId, cancellationToken);

                if (roleToAdd == null)
                {
                    roleToAdd = await this.rolesRepository.AddAsync(new Role
                    {
                        HebrewName = role.HebrewName,
                        Name = role.Name,
                        Immunity = role.Immunity,
                        ShowToAdmin = role.ShowToAdmin,
                        OrganizationId = command.OrganizationId
                    }, cancellationToken);
                }

                var userRole = await this.userRolesRepository.GetAsync(ur => ur.RoleId == roleToAdd.Id && ur.UserId == userId, cancellationToken);
                if (userRole == null)
                {
                    //throw new EntityValidationException(nameof(AddRoleToUserCommand.RolesId), $"Role with id {RoleId} already added", 17);
                    await this.userRolesRepository.AddAsync(new UserRole { RoleId = roleToAdd.Id, UserId = userId }, cancellationToken);
                }
            });

            return await this.mediator.Send(new GetUserFullInfoQuery(userId), cancellationToken);

        }
    }
}
