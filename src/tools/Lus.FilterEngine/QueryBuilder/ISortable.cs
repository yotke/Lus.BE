using System;
using System.Linq.Expressions;

namespace Lus.FilterEngine.QueryBuilder
{
    public interface ISortable<T> : IQueryParams<T>
    {
        IOrdered<T> SortedBy<TType>(Expression<Func<T, TType>> sortBy, bool inverseOrder);
        IOrdered<T> SortedBy(string sortBy, bool inverseOrder);

        ISortOrder<T> SortedBy<TType>(Expression<Func<T, TType>> sortBy);
        ISortOrder<T> SortedBy(string sortBy);

        // NEW: nested sort support (e.g., "Assignee" + "IdNumber").
        // Keeping the same method name; additional optional parameter.
        IOrdered<T> SortedBy(string sortBy, string? innerSortBy = null);

        IOrdered<T> SortedBy(string sortBy, bool IsDescending, string? innerSortBy = null);

    }
}
