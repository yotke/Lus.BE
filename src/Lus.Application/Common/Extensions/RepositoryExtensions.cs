using System.Linq.Expressions;
using Lus.Application.Common.Exceptions;

namespace Lus.Application.Common.Extensions
{
    public static class RepositoryExtensions
    {
        public static async Task<TEntity> GetSingleEntityAsync<TEntity, TEntityKey>(this IGenericRepository<TEntity, TEntityKey> repository, TEntityKey entityKey, CancellationToken cancellationToken = default)
               where TEntity : EntityBase<TEntityKey>
        {
            var entity = await repository.GetAsync(entityKey, cancellationToken);
            if (entity == null)
            {
                throw new EntityNotFoundException(typeof(TEntity).Name, int.Parse(entityKey == null ? "0" : entityKey.ToString()), 0);
            }

            return entity;
        }

        public static async Task<TEntity> GetSingleEntityAsync<TEntity, TEntityKey>(this IGenericRepository<TEntity, TEntityKey> repository, Expression<Func<TEntity, bool>> predicate, string errorMessage, CancellationToken cancellationToken = default)
            where TEntity : class
        {
            var entity = await repository.GetAsync(predicate, cancellationToken);
            if (entity == null)
            {
                throw new EntityNotFoundException(errorMessage);
            }

            return entity;
        }

        public static async Task<TEntity> GetSingleEntityAsync<TEntity, TProp, TEntityValue, TKey>(this IGenericRepository<TEntity, TKey> repository, Expression<Func<TEntity, TProp>> retriever, TEntityValue entityKey, CancellationToken cancellationToken = default)
            where TEntity : class
        {
            var predicate = CreateEqualsPredicate(retriever, entityKey);

            var entity = await repository.GetAsync(predicate, cancellationToken);
            return entity ?? throw new EntityNotFoundException(typeof(TEntity).Name, int.Parse(entityKey == null ? "0" : entityKey.ToString()), 0);
        }

        private static void ThrowIfNotMemberAccess(Expression body)
        {
            if (body.NodeType != ExpressionType.MemberAccess)
            {
                throw new ArgumentException($"Expected expression of type {ExpressionType.MemberAccess} but got {body.NodeType}");
            }
        }

        public static async Task ThrowOnEntityNotValidExceptionAsync<TEntity>(this IGenericRepository<TEntity, long> repository, params (Expression<Func<TEntity, string>> Retriever, string ValueToCheck, string ExceptionMessage)[] propertyCheckDescriptions)
                    where TEntity : class
        {
            if (propertyCheckDescriptions == null || !propertyCheckDescriptions.Any())
            {
                return;
            }

            var notUniqueProps = new Dictionary<string, string>();

            foreach (var (retriever, valueToCheck, exceptionMessage) in propertyCheckDescriptions)
            {
                ThrowIfNotMemberAccess(retriever.Body);

                var isUnique = await IsValueUniqueAsync(repository, retriever, valueToCheck);
                if (!isUnique)
                {
                    notUniqueProps.Add(((MemberExpression)retriever.Body).Member.Name, exceptionMessage);
                }
            }

            if (notUniqueProps.Any())
            {
                throw new EntityValidationException();
            }
        }

        private static async Task<bool> IsValueUniqueAsync<TEntity>(IGenericRepository<TEntity, long> repository, Expression<Func<TEntity, string>> retriever, string valueToCheck)
            where TEntity : class
        {
            var valueToCheckRetriever = PrepareValueToCheckRetriever(retriever, valueToCheck);
            var entity = await repository.GetAsync(valueToCheckRetriever);

            return entity == null;
        }

        private static Expression<Func<TEntity, bool>> PrepareValueToCheckRetriever<TEntity>(Expression<Func<TEntity, string>> retriever, string valueToCheck)
            where TEntity : class
        {
            var toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Array.Empty<Type>());

            var lowerStoredValue = Expression.Call(retriever.Body, toLowerMethod);
            var lowerCheckValue = Expression.Call(Expression.Constant(valueToCheck), toLowerMethod);
            var newBody = Expression.Equal(lowerStoredValue, lowerCheckValue);

            var newRetriever = Expression.Lambda<Func<TEntity, bool>>(newBody, retriever.Parameters);

            return newRetriever;
        }

        public static async Task ThrowOnEntityNotValidExceptionAsync<TEntity, TProp>(this IGenericRepository<TEntity, long> repository, Expression<Func<TEntity, TProp>> retriever, TProp valueToCheck, string filedName, string exceptionMessage)
            where TEntity : class
        {
            var predicate = CreateEqualsPredicate(retriever, valueToCheck);

            var entity = await repository.GetAsync(predicate);
            if (entity != null)
            {
                throw new EntityValidationException(filedName, exceptionMessage, 0);
            }
        }

        public static async Task ThrowOnEntityNotValidExceptionAsync<TEntity>(this IGenericRepository<TEntity, long> repository, Expression<Func<TEntity, bool>> predicate, string filedName, string exceptionMessage)
            where TEntity : class
        {
            var retrievedEntities = await repository.GetAllListAsync(predicate);
            if (retrievedEntities.Any())
            {
                throw new EntityValidationException(filedName, exceptionMessage, 0);
            }
        }

        private static Expression<Func<TEntity, bool>> CreateEqualsPredicate<TEntity, TProp, TEntityKey>(Expression<Func<TEntity, TProp>> retriever, TEntityKey entityKey)
        {
            ThrowIfNotMemberAccess(retriever.Body);
            var body = retriever.Body;
            var propertyToCompare = Expression.Property(Expression.Constant(new { entityKey }), nameof(entityKey));
            var newBody = Expression.Equal(body, propertyToCompare);
            var predicate = Expression.Lambda<Func<TEntity, bool>>(newBody, retriever.Parameters);
            return predicate;
        }
    }
}
