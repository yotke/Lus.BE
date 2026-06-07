using System;
using System.Linq.Expressions;
using System.Reflection;
using Lus.FilterEngine.Operations;

namespace Lus.FilterEngine.Builder
{
    public class CaseInsensitiveSearchRequestPredicateBuilder<T> : SearchRequestPredicateBuilder<T>
        where T : class
    {
        protected override Expression ComposeOperationExpression(
            MemberExpression property,
            FilterOperation operation,
            Expression valueExpression)
        {
            // If target member isn't string, delegate to base
            if (property.Type != typeof(string))
            {
                return base.ComposeOperationExpression(property, operation, valueExpression);
            }

            // Ensure value is a string expression
            Expression valueAsString = valueExpression.Type == typeof(string)
                ? valueExpression
                : Expression.Call(valueExpression, typeof(object).GetMethod(nameof(object.ToString))!);

            switch (operation)
            {
                case FilterOperation.Eq:
                    // property.Equals(value, OrdinalIgnoreCase)
                    return Expression.Call(
                        property,
                        GetStringInstanceMethod(nameof(string.Equals)),
                        valueAsString,
                        Expression.Constant(StringComparison.OrdinalIgnoreCase));

                case FilterOperation.Sw:
                    // property.StartsWith(value, OrdinalIgnoreCase)
                    return Expression.Call(
                        property,
                        GetStringInstanceMethod(nameof(string.StartsWith)),
                        valueAsString,
                        Expression.Constant(StringComparison.OrdinalIgnoreCase));

                case FilterOperation.Ct:
                    // property.IndexOf(value, OrdinalIgnoreCase) >= 0
                    var indexOfCall = Expression.Call(
                        property,
                        GetStringInstanceMethod(nameof(string.IndexOf)),
                        valueAsString,
                        Expression.Constant(StringComparison.OrdinalIgnoreCase));

                    return Expression.GreaterThanOrEqual(indexOfCall, Expression.Constant(0));

                default:
                    return base.ComposeOperationExpression(property, operation, valueExpression);
            }
        }

        private static MethodInfo GetStringInstanceMethod(string name) =>
            typeof(string).GetMethod(name, new[] { typeof(string), typeof(StringComparison) })
            ?? throw new InvalidOperationException($"Could not find string.{name}(string, StringComparison) overload.");
    }
}
