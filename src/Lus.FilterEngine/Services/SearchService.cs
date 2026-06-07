using AutoMapper;
using Lus.FilterEngine.Builder;
using Lus.FilterEngine.Models;
using Lus.FilterEngine.Persistence;
using Lus.FilterEngine.Common;
using Lus.FilterEngine.Converters;
using Lus.FilterEngine.QueryBuilder;
using Sorting = Lus.FilterEngine.Common.Sorting;

namespace Lus.FilterEngine.Services
{
    public class SearchService<TDto, TProjection> : ISearchService<TDto>
        where TDto : class
        where TProjection : class
    {
        private readonly ISearchRequestConverter<TDto, TProjection> converter;
        private readonly ISearchRequestPredicateBuilder<TProjection> builder;
        private readonly IMapper mapper;
        private readonly IDataRetriever<TProjection> retriever;

        public SearchService(IMapper mapper, IDataRetriever<TProjection> retriver)
            : this(
                 new DefaultSearchRequestConverter<TDto, TProjection>(),
                 new SearchRequestPredicateBuilder<TProjection>(),
        mapper,
        retriver)
        { }

        public SearchService(
            ISearchRequestConverter<TDto, TProjection> converter,
            ISearchRequestPredicateBuilder<TProjection> builder,
            IMapper mapper,
            IDataRetriever<TProjection> retriever)
        {
            this.converter = converter;
            this.builder = builder;
            this.mapper = mapper;
            this.retriever = retriever;
        }

        public async Task<FramedResultDto<TDto>> SearchAsync(SearchRequest<TDto> searchRequest, Sorting defaultSorting,
            CancellationToken cancellationToken = default)
        {
            AddDefaultSortingIfNeeded(searchRequest, defaultSorting);

            var request = this.converter.ConvertRequest(searchRequest);

            var queryWithPredicate = AttachPredicate(request);
            var queryWithSelect = AttachSelect(request, queryWithPredicate);
            var queryWithOrdering = AttachSorting(request, queryWithSelect);
            var builtQuery = AttachFraming(searchRequest, queryWithOrdering);

            var dataFrame = await builtQuery.RetrieveFrameUsingAsync(this.retriever, request.SkipCount ?? false, cancellationToken);

            return MapToFramedResultDto(dataFrame);
        }

        public async Task<FramedResultDto<TDto>> SearchAsync(SearchRequest<TDto> searchRequest, Sorting defaultSorting,
            Filtering defaultFiltering, CancellationToken cancellationToken = default)
        {
            if (!searchRequest.Filters.Any(f => f.PropertyName.Equals(defaultFiltering.PropertyName, StringComparison.OrdinalIgnoreCase)))
            {
                searchRequest.Filters.Add(defaultFiltering);
            }

            return await SearchAsync(searchRequest, defaultSorting, cancellationToken);
        }

        private FramedResultDto<TDto> MapToFramedResultDto(DataFrame<TProjection> dataFrame)
        {
            var items = dataFrame.Items.Select(p => this.mapper.Map<TDto>(p));
            return new FramedResultDto<TDto>(items, dataFrame.Count);
        }

        private static IQueryParams<TProjection> AttachFraming(SearchRequest<TDto> searchRequest, IOrdered<TProjection> queryWithOrdering)
        {
            var query = queryWithOrdering
                .Skip(searchRequest.Framing.Skip)
                .Take(searchRequest.Framing.Take);
            return query;
        }

        private static IOrdered<TProjection> AttachSorting(SearchRequest<TProjection> request, ISortable<TProjection> queryWithSelect)
        {
            foreach (var sorting in request.Sorts)
            {
                queryWithSelect = queryWithSelect.SortedBy(sorting.SortBy, sorting.InverseOrder, sorting.InnerSortBy ?? null);
            }

            var queryWithOrdering = queryWithSelect as IOrdered<TProjection>;
            return queryWithOrdering;
        }

        private ISortable<TProjection> AttachSelect(SearchRequest<TProjection> searchRequest, ISelectable<TProjection> queryWithPredicate)
        {
            if (searchRequest.Fields?.Any() ?? false)
            {
                queryWithPredicate = queryWithPredicate.Select(searchRequest.Fields.ToArray());
            }

            return queryWithPredicate;
        }

        private ISelectable<TProjection> AttachPredicate(SearchRequest<TProjection> request)
        {
            var predicate = this.builder.Build(request);

            var queryWithPredicate = Items<TProjection>.Where(predicate);
            return queryWithPredicate;
        }

        private static void AddDefaultSortingIfNeeded(SearchRequest<TDto> searchRequest, Sorting defaultSorting)
        {
            searchRequest.Sorts ??= new List<Sorting>();

            if (!searchRequest.Sorts.Any())
            {
                searchRequest.Sorts = new List<Sorting> { defaultSorting };
            }
            else if (searchRequest.Sorts is not List<Sorting>)
            {
                searchRequest.Sorts = searchRequest.Sorts.ToList();
            }

            if (searchRequest.Sorts.All(s => s.SortBy != "Id"))
            {
                searchRequest.Sorts.Add(new Sorting("Id", true));
            }
        }
    }
}
