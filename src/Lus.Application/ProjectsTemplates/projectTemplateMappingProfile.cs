using AutoMapper;
using Lus.Application.ProjectsTemplates.Commands.CreateProject;
using Lus.Application.ProjectsTemplates.Commands.ModifyProject;
using Lus.Application.ProjectsTemplates.Entities;
using Lus.Contracts.ProjectsTemplates;

namespace Lus.Application.ProjectsTemplates
{
    public class projectTemplateMappingProfile : Profile
    {
        public projectTemplateMappingProfile()
        {
            CreateMap<ProjectTemplate, ProjectTemplateDto>();

            CreateMap<ProjectTemplateDto, CreateProjectTemplateCommand>();
            CreateMap<ProjectTemplateDto, ProjectTemplate>()
           .ForMember(c => c.Id, opt => opt.Ignore());

            CreateMap<CreateProjectTemplateCommand, ProjectTemplate>();
            CreateMap<ModifyProjectTemplateCommand, ProjectTemplate>();
   
            CreateMap<CreateProjectTemplateDto, CreateProjectTemplateCommand>();

            CreateMap<ModifyProjectTemplateDto, ProjectTemplate>();
            CreateMap<ModifyProjectTemplateDto, ModifyProjectTemplateCommand>();


       
        }
    }
}