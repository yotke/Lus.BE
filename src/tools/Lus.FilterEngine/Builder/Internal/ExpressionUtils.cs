using System.Linq.Expressions;
using System.Reflection;

namespace Lus.FilterEngine.Builder.Internal
{
    internal static class ExpressionUtils
    {
        // Cache reflection results
        private static readonly MethodInfo Any2 = typeof(Enumerable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(Enumerable.Any) && m.GetParameters().Length == 2);

        public static bool IsEnumerableButNotString(Type t) =>
            typeof(System.Collections.IEnumerable).IsAssignableFrom(t) && t != typeof(string);

        public static Type GetEnumerableElementType(Type seqType)
        {
            if (seqType.IsArray) return seqType.GetElementType()!;
            var ienum = seqType.GetInterfaces()
                .Concat(new[] { seqType })
                .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (ienum == null) throw new ArgumentException($"Type {seqType} is not an IEnumerable<T>.");
            return ienum.GetGenericArguments()[0];
        }

        public static MemberExpression ResolveInnerPath(MemberExpression baseExpr, string? innerPath)
        {
            if (string.IsNullOrWhiteSpace(innerPath)) return baseExpr;
            Expression current = baseExpr;
            foreach (var segment in innerPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
                current = Expression.PropertyOrField(current, segment);
            return (MemberExpression)current;
        }

        public static MemberExpression ResolveInnerPathFromParameter(ParameterExpression param, string? innerPath)
        {
            if (string.IsNullOrWhiteSpace(innerPath))
                throw new ArgumentException("InnerPath must be provided for collection filters.");

            Expression current = param;
            foreach (var segment in innerPath.Split('.', StringSplitOptions.RemoveEmptyEntries))
                current = Expression.PropertyOrField(current, segment);
            return (MemberExpression)current;
        }

        /// <summary>
        /// Builds: collection.Any(e => elementPredicate(e))
        /// where elementPredicate is composed from filter parameters with their grouping.
        /// </summary>
        public static Expression BuildAnyPredicate(
            Expression collectionExpr,
            IEnumerable<Common.FilterParameter> parameters,
            Func<Common.FilterParameter, MemberExpression, Expression> makeComparison)
        {
            var elementType = GetEnumerableElementType(collectionExpr.Type);
            var elementParam = Expression.Parameter(elementType, "e");

            Expression? elementPredicate = null;
            foreach (var p in parameters)
            {
                var member = ResolveInnerPathFromParameter(elementParam, p.InnerPath);
                var part = makeComparison(p, member);
                elementPredicate = elementPredicate == null
                    ? part
                    : p.GroupingOperation switch
                    {
                        Operations.BooleanOperation.And => Expression.AndAlso(elementPredicate, part),
                        Operations.BooleanOperation.Or => Expression.OrElse(elementPredicate, part),
                        _ => throw new NotSupportedException("Not Supported Grouping operation")
                    };
            }

            var any = Any2.MakeGenericMethod(elementType);

            var ienumT = typeof(IEnumerable<>).MakeGenericType(elementType);
            var sourceExpr = ienumT.IsAssignableFrom(collectionExpr.Type)
                ? collectionExpr
                : Expression.Convert(collectionExpr, ienumT);

            var lambda = Expression.Lambda(elementPredicate ?? Expression.Constant(true), elementParam);
            return Expression.Call(any, sourceExpr, lambda);
        }
    }
}
