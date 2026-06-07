using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lus.Contracts;
using Lus.Contracts.ProjectsTimes;
using Lus.Application.ProjectsTimes.Commands.DeleteProjectTime;
using Lus.Application.ProjectsTimes.Queries.GetProjectTimesByProjectId;
using Lus.Application.ProjectsTimes.Commands.ModifyProjectTimes;
using Lus.Application.ProjectsTimes.Commands.CreateProjectsTime;
using Lus.Contracts.Common.Models;

namespace Lus.Controllers
{
    [Route("v1/ProjectsTimes")]
    [Consumes("application/json"), Produces("application/json")]
    [ApiController]
    public class ProjectsTimesController : Controller
    {
        private readonly IMediator mediator;
        private readonly IMapper mapper;

        public ProjectsTimesController(IMediator mediator, IMapper mapper)
            => (this.mediator, this.mapper) = (mediator, mapper);

        [HttpGet("GetAll/{ProjectTimeId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ICollection<ProjectTimeDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllProjectsTimes(int ProjectTimeId)
        {
            var ProjectTimes = await this.mediator.Send(new GetProjectTimesByProjectIdQuery(ProjectTimeId));

            return Ok(ProjectTimes);
        }

        [HttpPost("CreateTimes")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ICollection<ProjectTimeDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        [AllowAnonymous]
        public async Task<IActionResult> CreateProjectTimes(ICollection<CreateProjectTimeDto> requestsDto)
        {
            var projectTimesDto = await this.mediator.Send(this.mapper.Map<CreateProjectTimesCommand>(requestsDto));
           
            return Ok(projectTimesDto);
        }
        [HttpPost("ModifyTimes")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ICollection<ProjectTimeDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        [AllowAnonymous]
        public async Task<IActionResult> ModifyProjectTimes(ICollection<ModifyProjectTimeDto> requestsDto)
        {
            var projectTimesDto = await this.mediator.Send(new ModifyProjectTimesCommand(requestsDto));
            return Ok(projectTimesDto);
        }
        [HttpPost("Delete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        [AllowAnonymous]
        public async Task<IActionResult> DeleteProjectTime(IdName ProjectTime)
        {
            await this.mediator.Send(new DeleteProjectTimeCommand(ProjectTime.id));

            return NoContent();
        }
    }
}
