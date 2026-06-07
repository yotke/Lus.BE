namespace Lus.FilterEngine.Persistence
{
    public class DataFrame<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int Count { get; set; }
    }
}
