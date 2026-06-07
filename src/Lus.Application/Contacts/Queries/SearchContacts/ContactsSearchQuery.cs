using Lus.Contracts.Contacts;
using Lus.FilterEngine;
using Lus.FilterEngine.Models;
using MediatR;

namespace Lus.Application.Contacts.Queries.SearchContacts
{
    public record ContactsSearchQuery(SearchRequest<SearchContactDto> SearchRequest)
        : IRequest<FramedResultDto<SearchContactDto>>;
}
