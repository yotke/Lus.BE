using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lus.Application;
using Lus.Application.HtmlTemplates.Commands.CreateHtmlTemplate;
using Lus.Application.HtmlTemplates.Commands.ModifyHtmlTemplate;
using Lus.Application.HtmlTemplates.Commands.SendContactUsNotification;
using Lus.Application.HtmlTemplates.Commands.SendHtmlTemplateNotification;
using Lus.Application.HtmlTemplates.Queries.GetHtmlTemplatesByType;
using Lus.Contracts;
using Lus.Contracts.HtmlTemplates;

namespace Lus.Controllers
{
    [Route("v1/htmlTemplates")]
    [Consumes("application/json"), Produces("application/json")]
    [ApiController]
    [Authorize]
    public class HtmlTemplatesController : Controller
    {
        private readonly IMediator mediator;
        private readonly IMapper mapper;

        public HtmlTemplatesController(IMediator mediator, IMapper mapper)
            => (this.mediator, this.mapper) = (mediator, mapper);

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HtmlTemplateDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> SaveHtmlTemplate(CreateHtmlTemplateDto requestDto)
        {
            var htmlTemplatesDto = await this.mediator.Send(this.mapper.Map<CreateHtmlTemplateCommand>(requestDto));
            return Ok(htmlTemplatesDto);
        }

        [HttpPost("modify")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(HtmlTemplateDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> ModifyHtmlTemplate(ModifyHtmlTemplateDto requestDto)
        {
            var htmlTemplatesDto = await this.mediator.Send(this.mapper.Map<ModifyHtmlTemplateCommand>(requestDto));
            return Ok(htmlTemplatesDto);
        }

        [HttpPost("GetHtmlTemplatesByType")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ICollection<HtmlTemplateDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> GetAllHtmlTemplatesByType(GetHtmlTemplatesByTypeDto requestDto)
        {
            var htmlTemplatesDto = await this.mediator.Send(new GetHtmlTemplatesByTypeQuery(requestDto.HtmlType));

            return Ok(htmlTemplatesDto);
        }

        [HttpPost("SendNotificationDeleteFileByEmail")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ICollection<HtmlTemplateDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> SendNotificationDeleteFileByEmail(SendHtmlTemplateNotificationDto requestDto)
        {
            await this.mediator.Send(new SendHtmlTemplateNotificationCommand(requestDto.UserId, requestDto.TemplateData));

            return Ok();
        }

        [HttpPost("SendMailToUs")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ICollection<HtmlTemplateDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        [Authorize(AuthenticationSchemes = ApplicationConstants.AuthPolicies.BasicAuthentication)]
        public async Task<IActionResult> SendMailToUs(SendContactUsNotificationDto requestDto)
        {
            await this.mediator.Send(new SendContactUsNotificationCommand
            {
                Name = requestDto.Name,
                ReplayEmail = requestDto.ReplayEmail,
                TemplateData = requestDto.TemplateData,
                UpdatedById = requestDto.UpdatedById
            });

            return Ok();
        }

    }
}
