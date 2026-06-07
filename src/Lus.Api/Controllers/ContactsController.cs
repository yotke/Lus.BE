using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lus.Application.Contacts.Commands.DeleteContact;
using Lus.Application.Contacts.Queries;
using Lus.Application.Contacts.Queries.SearchContacts;
using Lus.Contracts;
using Lus.Contracts.Contacts;
using Lus.FilterEngine;
using Lus.FilterEngine.Models;

namespace Lus.Controllers
{
    [Route("v1/contacts")]
    [Consumes("application/json"), Produces("application/json")]
    [ApiController]
    [Authorize]
    public class ContactsController : Controller
    {
        private readonly IMediator mediator;
        private readonly IMapper mapper;

        public ContactsController(IMediator mediator, IMapper mapper)
            => (this.mediator, this.mapper) = (mediator, mapper);


        [HttpGet("{organizationId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ICollection<ContactDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> GetAllOrganizationContacts(int organizationId)
        {
            var contacts = await this.mediator.Send(new GetContactsQuery(organizationId));

            return Ok(contacts);
        }

        [HttpPost("search")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FramedResultDto<SearchContactDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> SearchContacts(SearchRequest<SearchContactDto> searchRequest)
        {
            var result = await this.mediator.Send(new ContactsSearchQuery(searchRequest));

            return Ok(result);
        }

        [HttpDelete("{contactId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorModel))]
        public async Task<IActionResult> DeleteContact(int contactId)
        {
            await this.mediator.Send(new DeleteContactCommand(contactId));

            return NoContent();
        }
    }
}
