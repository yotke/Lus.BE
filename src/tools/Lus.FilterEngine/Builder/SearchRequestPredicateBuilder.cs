using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using Lus.FilterEngine.Builder.Internal;
using Lus.FilterEngine.Common;
using Lus.FilterEngine.Operations;

namespace Lus.FilterEngine.Builder
{
    public class SearchRequestPredicateBuilder<T> : ISearchRequestPredicateBuilder<T>
        where T : class
    {
        private const int MaxPlaceholderCountForInOperation = 10;

        public Expression<Func<T, bool>> Build(SearchRequest<T> request)
        {
            if (!(request.Filters?.Any() ?? false))
            {
                return arg => true;
            }

            var inputParam = Expression.Parameter(typeof(T), "arg");

            var fieldFilterExpression = request.Filters.Aggregate(
                (Expression?)null,
                (accumulated, filter) =>
                {
                    var current = CreateFieldFilterExpression(inputParam, filter);
                    if (accumulated == null) return current;

                    return filter.GroupingOperation switch
                    {
                        BooleanOperation.And => Expression.AndAlso(current, accumulated),
                        BooleanOperation.Or => Expression.OrElse(current, accumulated),
                        _ => throw new NotSupportedException("Not Supported Grouping operation")
                    };
                });

            return fieldFilterExpression == null
                ? (Expression<Func<T, bool>>)(arg => true)
                : Expression.Lambda<Func<T, bool>>(fieldFilterExpression, inputParam);
        }

        private Expression CreateFieldFilterExpression(Expression inputParam, Filtering fieldFilterItem)
        {
            // 1) Custom profile?
            var customFilter = FilterProfile.TryGetFilter(typeof(T), fieldFilterItem.PropertyName);
            if (customFilter != null)
            {
                return customFilter(inputParam, fieldFilterItem);
            }

            // 2) Base property (e.g., "Assignee", "UserProcesses", "IsSubProcess", ...)
            var basePropertyExpr = Expression.PropertyOrField(inputParam, fieldFilterItem.PropertyName);

            // 3) If it's a collection: build Any(e => <composed predicate for element>)
            if (ExpressionUtils.IsEnumerableButNotString(basePropertyExpr.Type))
            {
                return ExpressionUtils.BuildAnyPredicate(
                    basePropertyExpr,
                    fieldFilterItem.FilterParameters,
                    (param, member) => CreateFieldFilterParamExpression(param, member)
                );
            }

            // 4) Scalar/object: compose predicates over the (possibly nested) member
            Expression? fieldFilterExpression = null;

            foreach (var filterParameter in fieldFilterItem.FilterParameters)
            {
                // Resolve nested member if InnerPath provided
                var targetMemberExpr = ExpressionUtils.ResolveInnerPath(
                    (MemberExpression)basePropertyExpr,
                    filterParameter.InnerPath
                );

                var expr = CreateFieldFilterParamExpression(filterParameter, targetMemberExpr);

                fieldFilterExpression = fieldFilterExpression == null
                    ? expr
                    : filterParameter.GroupingOperation switch
                    {
                        BooleanOperation.And => Expression.AndAlso(fieldFilterExpression, expr),
                        BooleanOperation.Or => Expression.OrElse(fieldFilterExpression, expr),
                        _ => throw new NotSupportedException("Not Supported Grouping operation")
                    };
            }

            return fieldFilterExpression!;
        }

        private static bool IsNullableOrReference(Type type)
            => !type.IsValueType || Nullable.GetUnderlyingType(type) != null;

        private static bool IsNullTokenForNonString(string? v)
            => string.IsNullOrEmpty(v) || string.Equals(v, "null", StringComparison.OrdinalIgnoreCase);

        private Expression CreateFieldFilterParamExpression(FilterParameter fieldOperationItem, MemberExpression propertyExpr)
        {
            var numberOfValues = fieldOperationItem.Values?.Count() ?? 0;

            // Null checks: no values expected
            if (fieldOperationItem.Operation == FilterOperation.IsNull)
            {
                if (!IsNullableOrReference(propertyExpr.Type))
                    throw new NotSupportedException($"IsNull not supported on non-nullable member {propertyExpr.Member.Name}");

                return Expression.Equal(propertyExpr, Expression.Constant(null, propertyExpr.Type));
            }

            if (fieldOperationItem.Operation == FilterOperation.IsNotNull)
            {
                if (!IsNullableOrReference(propertyExpr.Type))
                    throw new NotSupportedException($"IsNotNull not supported on non-nullable member {propertyExpr.Member.Name}");

                return Expression.NotEqual(propertyExpr, Expression.Constant(null, propertyExpr.Type));
            }

            // Values count validation (kept consistent with validator)
            if (numberOfValues == 0 || (numberOfValues > 1 && fieldOperationItem.Operation != FilterOperation.In))
            {
                throw new NotSupportedException("Not supported filter operation type.");
            }

            // Single-value ops
            if (fieldOperationItem.Operation != FilterOperation.In)
            {
                // Treat "" as NULL only for non-string members (front-end uses "" to mean null)
                var raw = fieldOperationItem.Values!.First()!;
                if (IsNullableOrReference(propertyExpr.Type) &&
                    propertyExpr.Type != typeof(string) &&
                    IsNullTokenForNonString(raw))
                {
                    var nullExpr = Expression.Constant(null, propertyExpr.Type);
                    var eqNull = Expression.Equal(propertyExpr, nullExpr);
                    return fieldOperationItem.IsNegated ? Expression.Not(eqNull) : eqNull;
                }

                var comparisonExpr = CreateComparisonOperationExpression(
                    propertyExpr,
                    fieldOperationItem.Operation,
                    raw,
                    fieldOperationItem.AsString ?? false
                );

                return fieldOperationItem.IsNegated ? Expression.Not(comparisonExpr) : comparisonExpr;
            }

            // IN operation (may include NULL token for non-string types)
            var treatEmptyAsNull = propertyExpr.Type != typeof(string);
            var hasNullToken = treatEmptyAsNull && fieldOperationItem.Values!.Any(IsNullTokenForNonString);

            // Split values: (nonNullValues, hasNullToken)
            var nonNullValues = (treatEmptyAsNull
                    ? fieldOperationItem.Values!.Where(v => !IsNullTokenForNonString(v))
                    : fieldOperationItem.Values!)
                .ToArray();

            Expression? inExpr;

            if (nonNullValues.Length == 0)
            {
                // Only NULL requested: property IS NULL
                inExpr = Expression.Equal(propertyExpr, Expression.Constant(null, propertyExpr.Type));
            }
            else if (nonNullValues.Length > MaxPlaceholderCountForInOperation)
            {
                // Use List<T>.Contains(property) for large sets
                inExpr = CreateContainsExpression(propertyExpr, nonNullValues);
                if (hasNullToken)
                {
                    var isNull = Expression.Equal(propertyExpr, Expression.Constant(null, propertyExpr.Type));
                    inExpr = Expression.OrElse(inExpr, isNull);
                }
            }
            else
            {
                // Or-chain of equals
                inExpr = nonNullValues
                    .Select(v => CreateComparisonOperationExpression(propertyExpr, FilterOperation.Eq, v!))
                    .Aggregate((Expression?)null, (curr, next) => curr == null ? next : Expression.OrElse(curr, next))!;

                if (hasNullToken)
                {
                    var isNull = Expression.Equal(propertyExpr, Expression.Constant(null, propertyExpr.Type));
                    inExpr = Expression.OrElse(inExpr, isNull);
                }
            }

            return fieldOperationItem.IsNegated ? Expression.Not(inExpr) : inExpr;
        }

        private Expression CreateContainsExpression(MemberExpression property, IEnumerable<string?> values)
        {
            var listType = typeof(List<>).MakeGenericType(property.Type);
            var containsMethod = listType.GetMethod("Contains", new[] { property.Type })
                                ?? throw new InvalidOperationException("Cannot find List<T>.Contains method.");

            var filterValues = Activator.CreateInstance(listType) as IList
                               ?? throw new InvalidOperationException("Cannot create List<T>.");

            foreach (var raw in values)
            {
                // raw is guaranteed non-null here
                filterValues.Add(ConvertValue(raw!, property.Type));
            }

            return Expression.Call(Expression.Constant(filterValues), containsMethod, property);
        }

        private Expression CreateComparisonOperationExpression(
    MemberExpression property,
    FilterOperation operation,
    string rawValue,
    bool asString = false)
        {
            // 1) Force string semantics ONLY for Ct/Sw when AsString is true
            if (asString && (operation == FilterOperation.Ct || operation == FilterOperation.Sw))
            {
                var memberStr = StringifyProperty(property);
                var valueExprS = Expression.Constant(rawValue ?? string.Empty, typeof(string));

                return operation == FilterOperation.Ct
                    ? Expression.Call(
                        memberStr,
                        typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })!,
                        valueExprS)
                    : Expression.Call(
                        memberStr,
                        typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) })!,
                        valueExprS);
            }

            // 2) Default path: convert raw to the member’s strong type (fixes Eq on bool? vs string etc.)
            var valueObj = ConvertValue(rawValue, property.Type);

            // Use the object's actual CLR type for the constant; then cast if needed to match the member
            var constActual = Expression.Constant(valueObj, valueObj?.GetType() ?? property.Type);

            // 👇 Make the conditional explicitly produce an Expression to avoid CS0173
            Expression valueExpr;
            if (constActual.Type == property.Type)
                valueExpr = constActual;                        // ConstantExpression : Expression
            else
                valueExpr = Expression.Convert(constActual, property.Type); // UnaryExpression : Expression

            return ComposeOperationExpression(property, operation, valueExpr);
        }

        protected virtual Expression ComposeOperationExpression(
        MemberExpression property,
        FilterOperation operation,
        Expression valueExpression)
        {
            // String ops: normalize both instance and argument to string
            if (operation == FilterOperation.Ct || operation == FilterOperation.Sw)
            {
                // Instance: property as string (handles int, bool?, DateTime?, etc.)
                Expression instance =
                    property.Type == typeof(string)
                        ? property
                        : StringifyProperty(property); // your helper: nullables => "", non-nullables => .ToString()

                // Argument: value as string
                Expression arg =
                    valueExpression.Type == typeof(string)
                        ? valueExpression
                        : Expression.Call(valueExpression, "ToString", Type.EmptyTypes);

                var method = typeof(string).GetMethod(
                    operation == FilterOperation.Ct ? nameof(string.Contains) : nameof(string.StartsWith),
                    new[] { typeof(string) })!;

                return Expression.Call(instance, method, arg);
            }

            // Typed ops stay as-is
            return operation switch
            {
                FilterOperation.Eq => Expression.Equal(property, valueExpression),
                FilterOperation.Gt => Expression.GreaterThan(property, valueExpression),
                FilterOperation.Lt => Expression.LessThan(property, valueExpression),
                _ => throw new NotSupportedException($"Operation {operation} is not supported here.")
            };
        }


        private static object ConvertValue(string stringValue, Type targetType)
        {
            // Strings: no conversion
            if (targetType == typeof(string))
            {
                return stringValue;
            }

            // Non-nullable
            if (!targetType.IsGenericType)
            {
                if (targetType.IsEnum)
                {
                    return Enum.Parse(targetType, stringValue, true);
                }

                if (targetType == typeof(TimeSpan))
                {
                    return TimeSpan.Parse(stringValue, CultureInfo.InvariantCulture);
                }

                var val = Convert.ChangeType(stringValue, targetType, CultureInfo.InvariantCulture);
                return AdjustTimeZoneIfDateTime(val);
            }

            // Nullable<T>
            var genericValueType = targetType.GetGenericTypeDefinition();
            if (genericValueType == typeof(Nullable<>))
            {
                var underlyingType = Nullable.GetUnderlyingType(targetType)!;

                if (string.IsNullOrEmpty(stringValue))
                {
                    // null
                    return null!;
                }

                object value;
                if (underlyingType == typeof(TimeSpan))
                {
                    value = TimeSpan.Parse(stringValue, CultureInfo.InvariantCulture);
                }
                else if (underlyingType.IsEnum)
                {
                    value = Enum.Parse(underlyingType, stringValue, true);
                }
                else
                {
                    value = Convert.ChangeType(stringValue, underlyingType, CultureInfo.InvariantCulture);
                    value = AdjustTimeZoneIfDateTime(value);
                }

                return value;
            }

            throw new NotSupportedException("Conversion not supported.");
        }

        private static object AdjustTimeZoneIfDateTime(object value)
        {
            if (value is DateTime dt)
            {
                return TimeZoneInfo.ConvertTimeToUtc(dt);
            }
            return value;
        }
        private static Expression StringifyProperty(MemberExpression property)
        {
            if (property.Type == typeof(string)) return property;

            var underlying = Nullable.GetUnderlyingType(property.Type);
            if (underlying != null)
            {
                // property.HasValue ? property.Value.ToString() : ""
                var hasValue = Expression.Property(property, "HasValue");
                var value = Expression.Property(property, "Value");
                var toStr = Expression.Call(value, "ToString", Type.EmptyTypes);

                return Expression.Condition(
                    hasValue,
                    toStr,
                    Expression.Constant(string.Empty, typeof(string))
                );
            }

            // Non-nullable: property.ToString()
            return Expression.Call(property, "ToString", Type.EmptyTypes);
        }
    }
}
