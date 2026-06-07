using EasyCaching.Core;
using Lus.Authorization;
using Lus.Contracts.Contacts;
using Lus.FilterEngine.Common;
using Lus.FilterEngine.Models;
using Lus.FilterEngine.Operations;
using Lus.FilterEngine.Services;
using MediatR;

namespace Lus.Application.Contacts.Queries.SearchContacts
{
    /// <summary>
    /// Runs a contact search through the filter engine, automatically enforcing
    /// tenant scoping: if the caller did not supply an OrganizationId filter, the
    /// current user's organization (cached) is injected so results never leak across tenants.
    /// </summary>
    public class ContactsSearchQueryHandler
        : IRequestHandler<ContactsSearchQuery, FramedResultDto<SearchContactDto>>
    {
        private readonly ISearchService<SearchContactDto> searchService;
        private readonly IEasyCachingProvider provider;
        private readonly IUserAccessor userAccessor;

        public ContactsSearchQueryHandler(
            ISearchService<SearchContactDto> searchService,
            IEasyCachingProvider provider,
            IUserAccessor userAccessor)
        {
            this.searchService = searchService;
            this.provider = provider;
            this.userAccessor = userAccessor;
        }

        public async Task<FramedResultDto<SearchContactDto>> Handle(
            ContactsSearchQuery request, CancellationToken cancellationToken)
        {
            var userId = this.userAccessor.ProjectUser.Id;

            var cachedOrg = await this.provider.GetAsync<int>(
                $"{ApplicationConstants.CachedProviderKeys.UserOrganizationCacheKey}{userId}",
                cancellationToken);
            var organizationId = cachedOrg.HasValue ? cachedOrg.Value : 0;

            request.SearchRequest.Filters ??= new List<Filtering>();

            // Tenant scoping: only inject if the caller didn't already filter by organization.
            if (!request.SearchRequest.Filters.Any(f =>
                    string.Equals(f.PropertyName, nameof(SearchContactDto.OrganizationId), StringComparison.OrdinalIgnoreCase)))
            {
                request.SearchRequest.Filters.Add(new Filtering
                {
                    GroupingOperation = BooleanOperation.And,
                    PropertyName = nameof(SearchContactDto.OrganizationId),
                    FilterParameters = new List<FilterParameter>
                    {
                        new FilterParameter
                        {
                            GroupingOperation = BooleanOperation.Or,
                            Operation = FilterOperation.Eq,
                            Values = new[] { organizationId.ToString() },
                            IsNegated = false
                        }
                    }
                });
            }

            return await this.searchService.SearchAsync(
                request.SearchRequest,
                new Sorting(nameof(SearchContactDto.Id), true),
                cancellationToken);
        }
    }
}
