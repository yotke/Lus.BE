using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lus.Application.Roles.Commands.CreateRole;
using Lus.Application.Roles.Commands.ModifyRoles;
using Lus.Application.Roles.Queries.GetRoles;
using Lus.Contracts;
using Lus.Contracts.Roles;

namespace Lus.Controllers
{
    [Route("v1/roles")]
    [Consumes("application/json"), Produces("application/json")]
    [ApiController]
    [Authorize]
    public class RolesController : Controller
    {
        private readonly IMediator mediator;
        private readonly IMapper mapper;

        public RolesController(IMediator mediator, IMapper mapper)
            => (this.mediator, this.mapper) = (mediator, mapper);


        [HttpGet("GetRoles")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ICollection<RoleDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> GetRoles()
        {
            var rolesDto = await this.mediator.Send(new GetRolesQuery(false));

            return Ok(rolesDto);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ICollection<RoleDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> GetAllRoles()
        {
            var rolesDto = await this.mediator.Send(new GetRolesQuery(true));

            return Ok(rolesDto);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> CreateRole(CreateRoleDto createRoleDto)
        {
            await this.mediator.Send(this.mapper.Map<CreateRoleCommand>(createRoleDto));

            return StatusCode(StatusCodes.Status201Created);
        }

        [HttpPost("ModifyRoles")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ICollection<RoleDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> ModifyRoles(ICollection<ModifyRoleDto> requestsDto)
        {
            var rolesDto = await this.mediator.Send(new ModifyRolesCommand(requestsDto));

            return Ok(rolesDto);
        }
    }
}
