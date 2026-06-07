namespace Lus.FilterEngine.Common
{
    public sealed class Sorting
    {
        public string SortBy { get; init; }
        public string? InnerSortBy { get; init; }
        public bool InverseOrder { get; init; }

        // Default constructor (fallback to Id ascending)
        public Sorting() : this("Id", false, null) { }

        // Explicit constructor
        public Sorting(string fieldName, bool isInverseOrder, string? innerFieldName = null)
        {
            SortBy = string.IsNullOrWhiteSpace(fieldName) ? "Id" : fieldName.Trim();
            InnerSortBy = string.IsNullOrWhiteSpace(innerFieldName) ? null : innerFieldName.Trim();
            InverseOrder = isInverseOrder;
        }
    }
}
