using System.Linq.Expressions;

namespace Lus.Application.Common.Extensions
{
    public interface IGenericRepository<TEntity, TPrimaryKey>
        where TEntity : class
    {
        Task RunStoredProcedureWithoutParameters(string nameOfStoredProcedure);

        Task TruncateTable(string tableName);

        Task<TEntity> GetAsync(TPrimaryKey id, CancellationToken cancelationToken = default);

        Task<TEntity> GetWithIncludeAsync(TPrimaryKey id, CancellationToken cancellationToken,
            params Expression<Func<TEntity, object>>[] includes);

        Task<TEntity> GetWithIncludeAsync(Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken, params Expression<Func<TEntity, object>>[] includes);

        Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancelationToken = default);

        Task<IEnumerable<TEntity>> GetAllListAsync(CancellationToken cancelationToken = default);

        Task<IEnumerable<TEntity>> GetAllListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancelationToken = default);

        Task<IEnumerable<TEntity>> GetAllListAsync(Expression<Func<TEntity, bool>> predicate = default,
            CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includes);

        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancelationToken = default);

        Task<TEntity> AddAsync(TEntity entity, CancellationToken cancelationToken = default);

        Task AddAllAsync(IEnumerable<TEntity> entities, CancellationToken cancelationToken = default);

        Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancelationToken = default);

        Task UpdateAllAsync(IEnumerable<TEntity> entities, CancellationToken cancelationToken = default);

        Task DeleteAsync(TEntity entity, CancellationToken cancelationToken = default);

        Task DeleteAllAsync(IEnumerable<TEntity> entities, CancellationToken cancelationToken = default);

        Task DeleteAllAsync(Expression<Func<TEntity, bool>> predicate = default, CancellationToken cancelationToken = default);
    }
}
