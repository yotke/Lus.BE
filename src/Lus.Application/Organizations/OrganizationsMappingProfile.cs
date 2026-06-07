using AutoMapper;
using Lus.Application.Organizations.Commands.CreateOrganization;
using Lus.Application.Organizations.Commands.ModifyOrganization;
using Lus.Application.Organizations.Entities;
using Lus.Application.Organizations.Queries.GetOrganizations;
using Lus.Application.Organizations.Queries.GetOrganizationsToUser;
using Lus.Contracts.Organizations;

namespace Lus.Application.Organizations
{
    public class OrganizationMappingProfile : Profile
    {
        public OrganizationMappingProfile()
        {
            CreateMap<CreateOrganizationDto, CreateOrganizationCommand>();

            CreateMap<Organization, OrganizationDto>();

            CreateMap<Organization, OrganizationInfoDto>();
            CreateMap<Organization, OrganizationToManageUserDto>();

            CreateMap<CreateOrganizationCommand, Organization>()
                .ForMember(c => c.Id, opt => opt.Ignore());

            CreateMap<ModifyOrganizationDto, ModifyOrganizationCommand>();


            CreateMap<SearchOrganizationQueryDto, GetOrganizationsQuery>()
                .ForMember(m => m.OrganizationId, opt => opt.MapFrom(s => s.OrganizationId.ToString()))
                .ForMember(m => m.AccountingNumber, opt => opt.MapFrom(s => s.AccountingId))
                .ForMember(m => m.Active, opt => opt.MapFrom(s => s.Active))
                .ForMember(m => m.AreaNumeriName, opt => opt.MapFrom(s => s.AreaId))
                .ForMember(m => m.CityNumeriId, opt => opt.MapFrom(s => s.CityId)); 
            
            CreateMap<SearchOrganizationQueryDto, GetOrganizationsToUserQuery>()
                .ForMember(m => m.OrganizationId, opt => opt.MapFrom(s => s.OrganizationId.ToString()))
                .ForMember(m => m.AccountingNumber, opt => opt.MapFrom(s => s.AccountingId))
                .ForMember(m => m.Active, opt => opt.MapFrom(s => s.Active))
                .ForMember(m => m.AreaNumeriName, opt => opt.MapFrom(s => s.AreaId))
                .ForMember(m => m.CityNumeriId, opt => opt.MapFrom(s => s.CityId));
        }
    }
}