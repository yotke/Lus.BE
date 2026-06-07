namespace Lus.FilterEngine.Converters
{
    public interface ISearchRequestConverter<TDto, T>
        where TDto : class
        where T : class
    {
        SearchRequest<T> ConvertRequest(SearchRequest<TDto> searchRequest);
    }
}
