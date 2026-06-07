using AutoMapper;
using MediatR;
using Lus.Application.ProjectsTemplates.Repositories;
using Lus.Contracts.ProjectsTemplates;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Lus.Application.ProjectsTemplates.Queries.GetMonthlyProjects
{
    public class GetMonthlyProjectsQueryHandler : IRequestHandler<GetMonthlyProjectsQuery, ICollection<ProjectTemplateDto>>
    {
        private readonly IProjectsTemplatesRepository projectsTemplatesRepository;
        private readonly IMapper mapper;

        public GetMonthlyProjectsQueryHandler(IProjectsTemplatesRepository projectsTemplatesRepository, IMapper mapper)
        {
            this.projectsTemplatesRepository = projectsTemplatesRepository;
            this.mapper = mapper;
        }

        public async Task<ICollection<ProjectTemplateDto>> Handle(GetMonthlyProjectsQuery request,
        CancellationToken cancellationToken)
        {
            var ProjectsTemplates = await projectsTemplatesRepository.GetAllListAsync(p => (p.CurrentDate.Year== request.monthDate.Year)&& p.CurrentDate.Month == request.monthDate.Month, cancellationToken, s => s.ProjectTimes);
            var ProjectsTemplatesDto = mapper.Map<ICollection<ProjectTemplateDto>>(ProjectsTemplates);

            return ProjectsTemplatesDto;
        }
        private bool areMonthAndYearEqual(DateTime date1, DateTime date2)
        {
            return date1.Year == date2.Year && date1.Month == date2.Month;
        }

    }
}
