using AutoMapper;
using MediatR;
using Lus.Application.Common.Extensions;
using Lus.Application.Roles.Entities;
using Lus.Application.Roles.Repositories;
using Lus.Contracts.Roles;

namespace Lus.Application.Roles.Commands.ModifyRoles
{
    public class ModifyRolesCommandHandler : IRequestHandler<ModifyRolesCommand, ICollection<RoleDto>>
    {
        private readonly IRolesRepository rolesRepository;
        private readonly IMapper mapper;

        private readonly List<string> roleListOfPropertiesToIgnore =
            new List<string> { "OrganizationId", "Organization", "UserRoles" };

        public ModifyRolesCommandHandler(IRolesRepository rolesRepository, IMapper mapper)
        {
            this.rolesRepository = rolesRepository;
            this.mapper = mapper;
        }

        public async Task<ICollection<RoleDto>> Handle(ModifyRolesCommand modifyCommand, CancellationToken cancellationToken)
        {
            var roles = new List<RoleDto>();
            foreach (var role in modifyCommand.Roles)
            {
                if (role.Id > 0)
                {
                    var savedRole =
                        await this.rolesRepository.GetSingleEntityAsync(role.Id, cancellationToken: cancellationToken);

                    savedRole.CopyIfDifferent(role, roleListOfPropertiesToIgnore);

                    roles.Add(this.mapper.Map<RoleDto>(
                        await this.rolesRepository.UpdateAsync(savedRole, cancellationToken)));
                }
                else
                {
                    roles.Add(this.mapper.Map<RoleDto>(
                        await this.rolesRepository.AddAsync(this.mapper.Map<Role>(role), cancellationToken)));
                }
            }
            await this.rolesRepository.RunSpCreateUpdateMuniRoles();

            return roles;
        }
    }
}