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

namespace Lus.Application.Users.Commands.ModifyRoleToUser
{
    public class ModifyRoleToUserCommandHandler : IRequestHandler<ModifyRoleToUserCommand, UserFullInfoDto>
    {
        private readonly IUserRolesRepository userRolesRepository;
        private readonly IMediator mediator;

        public ModifyRoleToUserCommandHandler(IUserRolesRepository userRolesRepository, IMediator mediator)
        {
            this.userRolesRepository = userRolesRepository;
            this.mediator = mediator;
        }

        public async Task<UserFullInfoDto> Handle(ModifyRoleToUserCommand command, CancellationToken cancellationToken)
        {
            var ur =await this.userRolesRepository.GetAllListAsync(ur => command.UserId == ur.UserId);
            var rolesToDelete = ur.Where(ur => command.roles.ToList().FindIndex(r=>r.Id==ur.RoleId)==-1);
            rolesToDelete.ToList().ForEach(roleToDelete =>
            {
                this.userRolesRepository.DeleteAsync(roleToDelete, cancellationToken);
            });
            command.roles.ToList().ForEach(async role =>
            {

                var userRoleToModify = await this.userRolesRepository.GetAsync(ur=> ur.RoleId==role.Id && ur.UserId == command.UserId, cancellationToken);
                if (userRoleToModify != null)
                {
                    await this.userRolesRepository.UpdateAsync(userRoleToModify, cancellationToken);
                }
                else
                {
                    await this.userRolesRepository.AddAsync(new UserRole { RoleId = role.Id, UserId = command.UserId }, cancellationToken);
                }
            });

            return await this.mediator.Send(new GetUserFullInfoQuery(command.UserId), cancellationToken);

        }
    }
}
