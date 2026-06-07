using System.Globalization;
using System.Linq.Expressions;
using Lus.FilterEngine.Common;

namespace Lus.FilterEngine.Builder
{
    public class FilterProfile
    {
        private static readonly Lazy<Dictionary<Type, Dictionary<string, Func<Expression, Filtering, Expression>>>>
            FilterConfig
                =
                new Lazy<Dictionary<Type, Dictionary<string, Func<Expression, Filtering, Expression>>>>(
                    () => new Dictionary<Type, Dictionary<string, Func<Expression, Filtering, Expression>>>());

        private static readonly Lazy<Dictionary<Type, HashSet<string>>> DtoRegistrations =
            new Lazy<Dictionary<Type, HashSet<string>>>(() => new Dictionary<Type, HashSet<string>>());

        public static void Initialize(params Type[] profiles)
        {
            foreach (var filterProfile in profiles)
            {
                Activator.CreateInstance(filterProfile);
            }
        }

        public void CreateFilter<TEntity, TDto>(string propertyName, Func<Expression, Filtering, Expression> expression)
        {
            if (FilterConfig.Value.TryGetValue(typeof(TEntity), out var propertyConfigs))
            {
                if (propertyConfigs.ContainsKey(propertyName))
                {
                    propertyConfigs[propertyName] = expression;
                }
                else
                {
                    propertyConfigs.Add(propertyName, expression);
                }
            }
            else
            {
                propertyConfigs =
                    new Dictionary<string, Func<Expression, Filtering, Expression>> { { propertyName, expression } };
                FilterConfig.Value.Add(typeof(TEntity), propertyConfigs);
            }

            if (DtoRegistrations.Value.TryGetValue(typeof(TDto), out var properties))
            {
                properties.Add(propertyName);
            }
            else
            {
                properties = new HashSet<string> { propertyName };
                DtoRegistrations.Value.Add(typeof(TDto), properties);
            }
        }

        internal static Func<Expression, Filtering, Expression>? TryGetFilter(Type type, string propertyName)
        {
            if (FilterConfig.Value.TryGetValue(type, out var propertyConfigs))
            {
                if (propertyConfigs.TryGetValue(propertyName, out var result))
                {
                    return result;
                }
            }
            return null;
        }

        internal static bool DtoPropertyFilterExists(Type type, string propertyName)
        {
            if (DtoRegistrations.Value.TryGetValue(type, out var properties))
            {
                return properties.Contains(propertyName);
            }

            return false;
        }

    }
}
