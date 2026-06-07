namespace Lus.FilterEngine.Models
{
    public class FramedResultDto<TProjectionDto>
    {
        public FramedResultDto()
            : this(items: Enumerable.Empty<TProjectionDto>(), count: 0)
        { }

        public FramedResultDto(IEnumerable<TProjectionDto> items, int count)
        {

            Items = items;
            Count = count;
        }

        public IEnumerable<TProjectionDto> Items { get; set; }
        public int Count { get; set; }
    }
}
