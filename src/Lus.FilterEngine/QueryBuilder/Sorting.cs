namespace Lus.FilterEngine.QueryBuilder
{
    internal class Sorting
    {
        public string FieldName { get; set; }
        public bool IsDescending { get; set; }
        public string? InnerFieldName { get; set; }

    }
}
