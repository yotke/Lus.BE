using EasyCaching.Core;
using Lus.Application.Contacts.Queries.SearchContacts;
using Lus.Authorization;
using Lus.Contracts.Contacts;
using Lus.FilterEngine;
using Lus.FilterEngine.Common;
using Lus.FilterEngine.Models;
using Lus.FilterEngine.Operations;
using Lus.FilterEngine.Services;
using Moq;
using Xunit;

namespace Lus.Api.Tests.Search
{
    /// <summary>
    /// Tenant-scoping is security-critical: the handler must force results to the
    /// current user's organization unless the caller explicitly scoped the query.
    /// </summary>
    public class ContactsSearchQueryHandlerTests
    {
        private static (ContactsSearchQueryHandler handler, List<SearchRequest<SearchContactDto>> captured)
            CreateHandler(int currentOrgId, int userId = 7)
        {
            var search = new Mock<ISearchService<SearchContactDto>>();
            var captured = new List<SearchRequest<SearchContactDto>>();
            search
                .Setup(s => s.SearchAsync(It.IsAny<SearchRequest<SearchContactDto>>(), It.IsAny<Sorting>(), It.IsAny<CancellationToken>()))
                .Callback<SearchRequest<SearchContactDto>, Sorting, CancellationToken>((req, _, _) => captured.Add(req))
                .ReturnsAsync(new FramedResultDto<SearchContactDto>(new List<SearchContactDto>(), 0));

            var provider = new Mock<IEasyCachingProvider>();
            provider
                .Setup(p => p.GetAsync<int>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CacheValue<int>(currentOrgId, true));

            var projectUser = new Mock<IProjectUser>();
            projectUser.Setup(u => u.Id).Returns(userId);
            var accessor = new Mock<IUserAccessor>();
            accessor.Setup(a => a.ProjectUser).Returns(projectUser.Object);

            return (new ContactsSearchQueryHandler(search.Object, provider.Object, accessor.Object), captured);
        }

        [Fact]
        public async Task Injects_current_organization_filter_when_caller_did_not()
        {
            var (handler, captured) = CreateHandler(currentOrgId: 42);
            var request = new SearchRequest<SearchContactDto> { Filters = new List<Filtering>() };

            await handler.Handle(new ContactsSearchQuery(request), CancellationToken.None);

            var sent = Assert.Single(captured);
            var orgFilter = Assert.Single(sent.Filters!, f => f.PropertyName == nameof(SearchContactDto.OrganizationId));
            var param = Assert.Single(orgFilter.FilterParameters);
            Assert.Equal(FilterOperation.Eq, param.Operation);
            Assert.Equal("42", Assert.Single(param.Values));
        }

        [Fact]
        public async Task Does_not_duplicate_or_override_caller_supplied_organization_filter()
        {
            var (handler, captured) = CreateHandler(currentOrgId: 42);
            var request = new SearchRequest<SearchContactDto>
            {
                Filters = new List<Filtering>
                {
                    new Filtering
                    {
                        PropertyName = nameof(SearchContactDto.OrganizationId),
                        GroupingOperation = BooleanOperation.And,
                        FilterParameters = new List<FilterParameter>
                        {
                            new FilterParameter
                            {
                                Operation = FilterOperation.Eq,
                                Values = new[] { "99" },
                                GroupingOperation = BooleanOperation.Or
                            }
                        }
                    }
                }
            };

            await handler.Handle(new ContactsSearchQuery(request), CancellationToken.None);

            var sent = Assert.Single(captured);
            var orgFilters = sent.Filters!.Where(f => f.PropertyName == nameof(SearchContactDto.OrganizationId)).ToList();
            Assert.Single(orgFilters);
            Assert.Equal("99", orgFilters[0].FilterParameters.First().Values.First());
        }
    }
}
