using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lus.Contracts;
using Lus.Application.ProjectsTemplates.Queries;
using Lus.Application.ProjectsTemplates.Commands.DeleteProject;
using Lus.Contracts.ProjectsTemplates;
using Lus.Application.ProjectsTemplates.Queries.GetAllProjects;
using Lus.Application.ProjectsTemplates.Queries.GetProjectsByOrganizationId;
using Lus.Application.ProjectsTemplates.Commands.CreateProject;
using Lus.Application.ProjectsTemplates.Commands.ModifyProject;
using Lus.Application.ProjectsTemplates.Queries.GetMonthlyProjects;
using Lus.Application.ProjectsTemplates.Entities;
using Lus.Contracts.ProjectsTimes.Models;

namespace Lus.Controllers
{
    [Route("v1/ProjectsTemplates")]
    [Consumes("application/json"), Produces("application/json")]
    [ApiController]
    public class ProjectsTemplatesController : Controller
    {
        private readonly IMediator mediator;
        private readonly IMapper mapper;

        public ProjectsTemplatesController(IMediator mediator, IMapper mapper)
            => (this.mediator, this.mapper) = (mediator, mapper);

        [HttpGet("GetProjects")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ICollection<ProjectTemplateDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllProjectsTemplates()
        {
            var ProjectTemplates = await this.mediator.Send(new GetProjectsTemplatesQuery());

            return Ok(ProjectTemplates);
        }
        [HttpPost("GetMonthlyProjects")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ICollection<ProjectTemplateDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        [AllowAnonymous]
        public async Task<IActionResult> GetMonthlyProjects(DateTimeDto ProjectCurrMonth)
        {
            //if (ProjectCurrMonth.HasValue)
            //{
                var ProjectTemplates = await this.mediator.Send(new GetMonthlyProjectsQuery(ProjectCurrMonth.CurrMonthDate));
                return Ok(ProjectTemplates);
            //}
            //return BadRequest("Invalid date format");
        }

        [HttpPost("Create")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProjectTemplateDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        [AllowAnonymous]
        public async Task<IActionResult> CreateProjectTemplate(CreateProjectTemplateDto requestDto)
        {
            var ProjectTemplatesDto = await this.mediator.Send(this.mapper.Map<CreateProjectTemplateCommand>(requestDto));
            return Ok(ProjectTemplatesDto);
        }
        [HttpPost("Modify")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProjectTemplateDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        [AllowAnonymous]
        public async Task<IActionResult> ModifyProjectTemplate(ModifyProjectTemplateDto requestDto)
        {
            var ProjectTemplatesDto = await this.mediator.Send(this.mapper.Map<ModifyProjectTemplateCommand>(requestDto));
            return Ok(ProjectTemplatesDto);
        }

        [HttpGet("{organizationId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ICollection<ProjectTemplateDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        [Authorize]

        public async Task<IActionResult> GetAllOrganizationProjectTemplates(int organizationId)
        {
            var ProjectTemplates = await this.mediator.Send(new GetProjectsTemplatesQueryByOrgId(organizationId));

            return Ok(ProjectTemplates);
        }

        [HttpPost("Delete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        [Authorize]
        public async Task<IActionResult> DeleteProjectTemplate(int ProjectTemplateId)
        {
            await this.mediator.Send(new DeleteProjectTemplateCommand(ProjectTemplateId));

            return NoContent();
        }
    }
}
