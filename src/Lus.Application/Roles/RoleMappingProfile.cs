using AutoMapper;
using Lus.Application.Roles.Commands.CreateRole;
using Lus.Application.Roles.Entities;
using Lus.Contracts.Roles;

namespace Lus.Application.Roles
{
    public class RoleMappingProfile : Profile
    {
        public RoleMappingProfile()
        {
            CreateMap<Role, RoleDto>();
            CreateMap<RoleDto, Role>();
            CreateMap<CreateRoleDto, CreateRoleCommand>();
            CreateMap<CreateRoleCommand, Role>();
            CreateMap<ModifyRoleDto, Role>()
                .ForMember(o => o.Id, opt => opt.Ignore());
        }
    }
}