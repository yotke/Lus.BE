using Lus.Application.Contacts.Projections;
using Lus.Contracts.Contacts;
using Lus.FilterEngine.Persistence;
using Lus.FilterEngine.Services;
using Lus.Infrastructure.Retrievers;
using Microsoft.Extensions.DependencyInjection;

namespace Lus.Infrastructure.Extensions
{
    /// <summary>
    /// Registers projection retrievers and their matching search services.
    /// Add one retriever + one search service per searchable entity (see docs/ORG_PROJECTIONS_SEARCH.md).
    /// </summary>
    public static class RetrieversExtensions
    {
        public static IServiceCollection AddRetrievers(this IServiceCollection services)
        {
            // Contacts (org-scoped)
            services.AddScoped<IDataRetriever<ContactProjection>, ContactRetriever>();
            services.AddScoped<ISearchService<SearchContactDto>, SearchService<SearchContactDto, ContactProjection>>();

            return services;
        }
    }
}
