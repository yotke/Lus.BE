using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Lus.FilterEngine.QueryBuilder
{
    internal sealed class FramedQueryContext<T> : IFilterable<T>, ISortable<T>, ISortOrder<T>, IOrdered<T>
    {
        private readonly List<Expression<Func<T, bool>>> predicates = new();
        private string[]? SelectedFields { get; set; }

        // NOTE: This Sorting is the QueryBuilder's internal one (not Common.Sorting).
        private readonly List<Sorting> sorting = new();
        private int? Skip { get; set; }
        private int? Take { get; set; }

        IQueryable<T> IQueryParams<T>.Apply(IQueryable<T> query, bool filterOnly)
        {
            var resultingQuery = query;

            foreach (var predicate in this.predicates)
                resultingQuery = resultingQuery.Where(predicate);

            if (filterOnly) return resultingQuery;

            if (SelectedFields?.Any() ?? false)
                resultingQuery = AddSelect(resultingQuery);

            for (var i = 0; i < this.sorting.Count; i++)
                resultingQuery = AddSorting(resultingQuery, this.sorting[i], isFirst: i == 0);

            if (Skip.HasValue) resultingQuery = resultingQuery.Skip(Skip.Value);
            if (Take.HasValue) resultingQuery = resultingQuery.Take(Take.Value);

            return resultingQuery;
        }

        ISelectable<T> ISelectable<T>.Select(string[] fields)
        {
            SelectedFields = fields;
            return this;
        }

        // ---------------- sort registration ----------------

        IOrdered<T> ISortable<T>.SortedBy<TType>(Expression<Func<T, TType>> sortBy, bool inverseOrder)
        {
            GuardSortByExpresion(sortBy, nameof(sortBy));
            var propertyName = GetPropertyName(sortBy);
            this.sorting.Add(new Sorting { FieldName = propertyName, IsDescending = inverseOrder });
            return this;
        }

        IOrdered<T> ISortable<T>.SortedBy(string sortBy, bool inverseOrder)
        {
            this.sorting.Add(new Sorting { FieldName = sortBy, IsDescending = inverseOrder });
            return this;
        }

        ISortOrder<T> ISortable<T>.SortedBy<TType>(Expression<Func<T, TType>> sortBy)
        {
            GuardSortByExpresion(sortBy, nameof(sortBy));
            var propertyName = GetPropertyName(sortBy);
            this.sorting.Add(new Sorting { FieldName = propertyName, IsDescending = false });
            return this;
        }

        // Keep original overload
        ISortOrder<T> ISortable<T>.SortedBy(string sortBy)
        {
            this.sorting.Add(new Sorting { FieldName = sortBy, IsDescending = false });
            return this;
        }

        // NEW overload with inner field support (e.g., "Assignee" + "IdNumber")
        IOrdered<T> ISortable<T>.SortedBy(string sortBy, string? innerSortBy)
        {
            this.sorting.Add(new Sorting
            {
                FieldName = sortBy,
                InnerFieldName = innerSortBy,
                IsDescending = false
            });
            return this;
        }
        IOrdered<T> ISortable<T>.SortedBy(string sortBy, bool isDescending, string? innerSortBy)
        {
            this.sorting.Add(new Sorting
            {
                FieldName = sortBy,
                InnerFieldName = innerSortBy,
                IsDescending = isDescending
            });
            return this;
        }

        private static string GetPropertyName<TType>(Expression<Func<T, TType>> sortBy)
            => (sortBy.Body as MemberExpression)!.Member.Name;

        private void GuardSortByExpresion<TType>(Expression<Func<T, TType>> sortBy, string argumentName)
        {
            if (sortBy.Body.NodeType != ExpressionType.MemberAccess)
                throw new ArgumentException(argumentName);
        }

        IFilterable<T> IFilterable<T>.Where(Expression<Func<T, bool>> predicate)
        {
            this.predicates.Add(predicate);
            return this;
        }

        IQueryParams<T> ITakeable<T>.First() { Skip = 0; Take = 1; return this; }
        IQueryParams<T> IFramable<T>.All() { Skip = Take = null; return this; }

        ITakeable<T> IFramable<T>.Skip(int num)
        {
            if (num < 0) throw new ArgumentException($"Argument {nameof(num)} should be greater than zero.");
            Skip = num; return this;
        }

        IQueryParams<T> ITakeable<T>.Take(int num)
        {
            if (num <= 0) throw new ArgumentException($"Argument {nameof(num)} should be greater or equal to zero.");
            Take = num; return this;
        }

        IOrdered<T> ISortOrder<T>.Asc() { this.sorting.Last().IsDescending = false; return this; }
        IOrdered<T> ISortOrder<T>.Desc() { this.sorting.Last().IsDescending = true; return this; }

        // ---------------- SELECT (unchanged) ----------------

        private IQueryable<T> AddSelect(IQueryable<T> query)
        {
            var parameter = Expression.Parameter(typeof(T), "x");

            var init = Expression.MemberInit(
                Expression.New(typeof(T)),
                SelectedFields!.Select(f =>
                {
                    var pi = typeof(T).GetProperty(
                        f, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                        ?? throw new InvalidOperationException($"Field '{f}' not found on {typeof(T).Name}");
                    return Expression.Bind(pi, Expression.Property(parameter, pi));
                }));

            return query.Select(Expression.Lambda<Func<T, T>>(init, parameter));
        }

        // ---------------- SORTING (supports inner field & dot paths) ----------------

        private static string ComposePath(Sorting s)
            => !string.IsNullOrWhiteSpace(s.InnerFieldName) ? $"{s.FieldName}.{s.InnerFieldName}" : s.FieldName;

        private static Expression BuildMemberAccess(Expression param, string path)
        {
            // supports "Assignee.IdNumber" or simple "Name"
            Expression cur = param;
            foreach (var part in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                var pi = cur.Type.GetProperty(part, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (pi == null)
                    throw new InvalidOperationException(
                        $"Property '{part}' not found on '{cur.Type.Name}' in path '{path}'.");
                cur = Expression.Property(cur, pi);
            }
            return cur;
        }

        private IQueryable<T> AddSorting(IQueryable<T> query, Sorting s, bool isFirst)
        {
            var queryExpr = query.Expression;
            var param = Expression.Parameter(typeof(T), "r");

            // If caller already passed a dot path in FieldName, we honor it.
            var path = s.FieldName.Contains('.') && string.IsNullOrWhiteSpace(s.InnerFieldName)
                ? s.FieldName
                : ComposePath(s);

            var member = BuildMemberAccess(param, path);

            // Optional: make string sorts null-safe for DB providers that dislike NULL ordering
            if (member.Type == typeof(string))
                member = Expression.Coalesce(member, Expression.Constant(string.Empty));

            var lambda = Expression.Lambda(member, param);

            var method = isFirst
                ? (s.IsDescending ? "OrderByDescending" : "OrderBy")
                : (s.IsDescending ? "ThenByDescending" : "ThenBy");

            queryExpr = Expression.Call(
                typeof(Queryable),
                method,
                new[] { typeof(T), member.Type },
                queryExpr,
                Expression.Quote(lambda));

            return query.Provider.CreateQuery<T>(queryExpr);
        }


    }
}
