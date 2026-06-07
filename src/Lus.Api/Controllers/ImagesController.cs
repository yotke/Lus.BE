using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lus.Application;
using Lus.Application.Images.Commands.DeleteImageById;
using Lus.Application.Images.Queries.GetImageById;
using Lus.Application.Images.Queries.GetPdfImageById;
using Lus.Application.Roles.Commands.ModifyImage;
using Lus.Contracts;

using Lus.Contracts.Images;

namespace Lus.Controllers
{
    [Route("v1/images")]
    [Consumes("application/json"), Produces("application/json")]
    [ApiController]
    [Authorize]
    public class ImagesController : Controller
    {
        private readonly IMediator mediator;
        private readonly IMapper mapper;

        public ImagesController(IMediator mediator, IMapper mapper)
            => (this.mediator, this.mapper) = (mediator, mapper);

        [HttpGet("{imageId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ImageDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> GetImageById(string imageId)
        {
            var image = await this.mediator.Send(new GetImageByIdQuery(imageId));

            return Ok(image);
        }

        [HttpDelete("{imageId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> DeleteImageById(string imageId)
        {
            await this.mediator.Send(new DeleteImageByIdCommand(imageId));

            return Ok();
        }

        //[HttpGet("{imageId}/ShowImage")]
        //[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ICollection<CandidateDto>))]
        //[ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        //[Authorize(AuthenticationSchemes = ApplicationConstants.AuthPolicies.BasicAuthentication)]
        //public async Task<IActionResult> GetPdfImage(string imageId)
        //{
        //    return await this.mediator.Send(new GetPdfImageByIdQuery(imageId));
        //} 
        [HttpPut("ModifyStatus")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        [Authorize(AuthenticationSchemes = ApplicationConstants.AuthPolicies.BasicAuthentication)]
        public async Task<IActionResult> GetPdfImage(ModifyImageStatusDto requestDto)
        {
            return Ok(await this.mediator.Send(this.mapper.Map<ModifyImageStatusCommand>(requestDto)));
           
        }
    }
}
