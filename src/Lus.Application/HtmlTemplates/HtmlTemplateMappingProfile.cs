using AutoMapper;
using Lus.Application.HtmlTemplates.Commands.CreateHtmlTemplate;
using Lus.Application.HtmlTemplates.Commands.ModifyHtmlTemplate;
using Lus.Application.HtmlTemplates.Entities;
using Lus.Contracts.HtmlTemplates;

namespace Lus.Application.HtmlTemplates
{
    public class HtmlTemplateMappingProfile : Profile
    {
        public HtmlTemplateMappingProfile()
        {
            CreateMap<CreateHtmlTemplateDto, CreateHtmlTemplateCommand>();

            CreateMap<CreateHtmlTemplateCommand, HtmlTemplate>();
            CreateMap<ModifyHtmlTemplateCommand, HtmlTemplate>();

            CreateMap<HtmlTemplate, HtmlTemplateDto>();
            CreateMap<HtmlTemplate, HtmlTemplateNotificationDto>();
            CreateMap<ModifyHtmlTemplateDto, ModifyHtmlTemplateCommand>();
        }
    }
}