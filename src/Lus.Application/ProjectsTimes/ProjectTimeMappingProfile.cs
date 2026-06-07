using AutoMapper;
using Lus.Application.ProjectsTimes.Commands.CreateProjectTime;
using Lus.Application.ProjectsTimes.Commands.ModifyProjectTime;
using Lus.Application.ProjectsTimes.Entities;
using Lus.Contracts.ProjectsTimes;
using Lus.Contracts.ProjectsTimes.Models;
using Lus.Contracts.Users;
using Newtonsoft.Json;

namespace Lus.Application.ProjectsTimes
{
    public class ProjectTimeMappingProfile : Profile
    {
        public ProjectTimeMappingProfile()
        {
            CreateMap<ProjectTime, ProjectTimeDto>()
   .ForMember(t => t.JsonTime, opt => opt.MapFrom(t => t.TimeData))
   .ForMember(t => t.TimesArray, opt => opt.MapFrom(t => string.IsNullOrWhiteSpace(t.TimeData) ? null : ToClass<TimesArrayDto>(t.TimeData)));


            CreateMap<ProjectTimeDto, ProjectTime>();

            CreateMap<ModifyProjectTimeDto, ModifyProjectTimeCommand>()
       .ForMember(t => t.TimeData, opt => opt.MapFrom(t => t.JsonTime));
            CreateMap<ModifyProjectTimeDto, ProjectTime>()
         .ForMember(t => t.TimeData, opt => opt.MapFrom(t => t.JsonTime));

            CreateMap<ModifyProjectTimeCommand, ProjectTime>()
       .ForMember(c => c.Id, opt => opt.Ignore());
            CreateMap<CreateProjectTimeCommand, ProjectTime>();

            CreateMap<CreateProjectTimeDto, CreateProjectTimeCommand>()
                  .ForMember(t => t.TimeData, opt => opt.MapFrom(t => t.JsonTime));
            CreateMap<CreateProjectTimeDto, ProjectTime>()
          .ForMember(t => t.TimeData, opt => opt.MapFrom(t => t.JsonTime));





        }

        private T ToClass<T>(string json)
        {
            var serializerSettings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
            return JsonConvert.DeserializeObject<T>(json, serializerSettings);
        }
    }
}