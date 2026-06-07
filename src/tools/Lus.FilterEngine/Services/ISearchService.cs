using Lus.FilterEngine.Common;
using Lus.FilterEngine.Models;

namespace Lus.FilterEngine.Services
{
    public interface ISearchService<TDto>
        where TDto : class
    {
        Task<FramedResultDto<TDto>> SearchAsync(SearchRequest<TDto> searchRequest, Sorting defaultSorting,
            CancellationToken cancellationToken = default);

        Task<FramedResultDto<TDto>> SearchAsync(SearchRequest<TDto> searchRequest, Sorting defaultSorting,
            Filtering defaultFiltering, CancellationToken cancellationToken = default);
    }
}
