using MassTransit.Internals;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;
using System.Reflection;

namespace Lus.Infrastructure.Options
{
    public class UpdateEntitiesOptions : IOptions<UpdateEntitiesOptions>
    {
        private readonly Dictionary<Type, object> configurationProviders = new();


        public UpdatableEntityPropertiesConfigurator<TEntity> ForEntity<TEntity>()
            where TEntity : class, new()
        {
            var configurationProvider = new UpdatableEntityPropertiesConfigurator<TEntity>();
            this.configurationProviders[typeof(TEntity)] = configurationProvider;

            return configurationProvider;
        }

        public IEnumerable<Func<TEntity, TEntity, bool>> UpdateFunctions<TEntity>()
            where TEntity : class, new()
        {
            if (this.configurationProviders.ContainsKey(typeof(TEntity)))
            {
                return ((UpdatableEntityPropertiesConfigurator<TEntity>)this.configurationProviders[
                    typeof(TEntity)]).UpdateFunctions;
            }

            return Enumerable.Empty<Func<TEntity, TEntity, bool>>();
        }

        public IEnumerable<PropertyInfo> UpdatableProperties<TEntity>()
            where TEntity : class, new()
        {
            if (this.configurationProviders.ContainsKey(typeof(TEntity)))
            {
                return ((UpdatableEntityPropertiesConfigurator<TEntity>)this.configurationProviders[
                    typeof(TEntity)]).UpdatableProperties;
            }

            return Enumerable.Empty<PropertyInfo>();
        }

        public UpdateEntitiesOptions Value => this;
    }

    public class UpdatableEntityPropertiesConfigurator<TEntity>
        where TEntity : class, new()
    {
        private readonly Dictionary<PropertyInfo, Func<TEntity, TEntity, bool>> entityPropertiesToUpdate;

        public UpdatableEntityPropertiesConfigurator() =>
            this.entityPropertiesToUpdate = typeof(TEntity).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite).ToList().ToDictionary(prop => prop, CreateFunc, new PropertyInfoComparer());

        private Func<TEntity, TEntity, bool> CreateFunc(PropertyInfo propertyInfo)
        {
            var storedEntity = Expression.Parameter(typeof(TEntity), "stored");
            var updatedEntity = Expression.Parameter(typeof(TEntity), "updated");
            var returnValue = Expression.Parameter(typeof(bool), "returnValue");

            var getter = propertyInfo.GetMethod;
            var setter = propertyInfo.SetMethod;

            var storedValue = Expression.Call(storedEntity, getter);
            var updatedValue = Expression.Call(updatedEntity, getter);

            var notEqual = Expression.NotEqual(storedValue, updatedValue);
            var initializeReturnValue = Expression.Assign(returnValue, Expression.IsTrue(notEqual));
            var updateAction = Expression.Call(storedEntity, setter, updatedValue);
            var result = Expression.IfThen(notEqual, updateAction);

            var body = Expression.Block(
                typeof(bool),
                new[] { returnValue },
                initializeReturnValue,
                result,
                returnValue);

            var returnFunction = Expression.Lambda<Func<TEntity, TEntity, bool>>(body, storedEntity, updatedEntity).CompileFast();

            return returnFunction;
        }

        public UpdatableEntityPropertiesConfigurator<TEntity> IgnoreProperty<TProperty>(
            Expression<Func<TEntity, TProperty>> propertyToIgnoreExp)
        {
            var propertyInfo = (propertyToIgnoreExp.Body as MemberExpression)?.Member as PropertyInfo;
            if (propertyInfo == null)
            {
                throw new InvalidOperationException("Provided member is not property");
            }

            this.entityPropertiesToUpdate.Remove(propertyInfo);

            return this;
        }

        public void IncludeAll()
        {
        }

        internal IEnumerable<Func<TEntity, TEntity, bool>> UpdateFunctions => this.entityPropertiesToUpdate.Values.AsEnumerable();

        internal IEnumerable<PropertyInfo> UpdatableProperties => this.entityPropertiesToUpdate.Keys.AsEnumerable();

        private class PropertyInfoComparer : IEqualityComparer<PropertyInfo>
        {
            public bool Equals(PropertyInfo x, PropertyInfo y) =>
                (x?.DeclaringType?.FullName, x?.PropertyType.FullName, x?.Name) ==
                (y?.DeclaringType?.FullName, y?.PropertyType.FullName, y?.Name);

            public int GetHashCode(PropertyInfo obj) => (obj.DeclaringType?.FullName, obj.PropertyType.FullName, obj.Name).GetHashCode();
        }
    }
}
