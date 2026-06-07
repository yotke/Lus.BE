using System.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Lus.Application.Common;
using Lus.Application.Common.Extensions;
using Lus.Authorization;
using Lus.Infrastructure.Extensions;
using System.Linq.Expressions;
using System.Data;
using MySql.Data.MySqlClient;

namespace Lus.Infrastructure.Repositories
{
    public abstract class EntityFrameworkRepository<TEntity, TPrimaryKey> : IGenericRepository<TEntity, TPrimaryKey>
        where TEntity : class
    {
        protected EntityFrameworkRepository(DbContext context, IUserAccessor userAccessor)
        {
            Context = context;
            UserAccessor = userAccessor;
        }

        protected DbContext Context { get; }

        protected DbSet<TEntity> Set => Context.Set<TEntity>();

        public async Task RunStoredProcedureWithoutParameters(string nameOfStoredProcedure)
        {
            await using MySqlConnection conn = new MySqlConnection(Context.Database.GetConnectionString());
            await conn.OpenAsync();

            MySqlCommand objCmd = conn.CreateCommand();
            objCmd.CommandType = CommandType.StoredProcedure;
            objCmd.CommandText = nameOfStoredProcedure;
            await objCmd.ExecuteNonQueryAsync();
        }

        protected IUserAccessor UserAccessor { get; }

        public virtual async Task TruncateTable(string tableName)
        {
            var command = $"TRUNCATE TABLE \"{tableName}\";";
            await Context.Database.ExecuteSqlRawAsync(command);
        }

        public virtual async Task<TEntity> GetAsync(TPrimaryKey id, CancellationToken cancellationToken = default)
        {
            if (typeof(IEntityWithKey<TPrimaryKey>).IsAssignableFrom(typeof(TEntity)))
            {
                return await Set.SingleOrDefaultAsync(x => ((IEntityWithKey<TPrimaryKey>)x).Id.Equals(id),
                    cancellationToken);
            }

            var entity = await Set.FindAsync(new object[] { id }, cancellationToken: cancellationToken);
            return entity;
        }

        public virtual async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            var entity = await Set.Where(predicate).FirstOrDefaultAsync(cancellationToken);
            return entity;
        }

        public async Task<TEntity> GetWithIncludeAsync(TPrimaryKey id, CancellationToken cancellationToken, params Expression<Func<TEntity, object>>[] includes)
        {
            var query = IncludeAllQuery(Set, includes);
            return await query.FirstOrDefaultAsync(x => ((IEntityWithKey<TPrimaryKey>)x).Id.Equals(id), cancellationToken);
        }

        public async Task<TEntity> GetWithIncludeAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken, params Expression<Func<TEntity, object>>[] includes)
        {
            var query = IncludeAllQuery(Set, includes);
            return await query.Where(predicate).FirstOrDefaultAsync(cancellationToken);
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllListAsync(CancellationToken cancellationToken = default)
        {
            var entities = await Set.ToListAsync(cancellationToken);
            return entities;
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllListAsync(Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken)
        {
            var entities = await Set.Where(predicate).ToListAsync(cancellationToken);
            return entities;
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllListAsync(Expression<Func<TEntity, bool>> predicate = default, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includes)
        {
            var querySelect = WithPredicate(Set, predicate);
            var query = IncludeAllQuery(querySelect, includes);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default) => await Set.Where(predicate).AnyAsync(cancellationToken);

        public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            InternalAdd(entity);
            await SaveChangesAsync(cancellationToken);
            return entity;
        }

        public virtual async Task AddAllAsync(IEnumerable<TEntity> entities,
            CancellationToken cancellationToken = default)
        {
            if (entities.EmptyIfNull().Any())
            {
                foreach (var entity in entities)
                {
                    InternalAdd(entity);
                }

                await SaveChangesAsync(cancellationToken);
            }
        }

        public virtual async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            InternalUpdate(entity);
            await SaveChangesAsync(cancellationToken);
            return entity;
        }

        public virtual async Task UpdateAllAsync(IEnumerable<TEntity> entities,
            CancellationToken cancellationToken = default)
        {
            if (entities.EmptyIfNull().Any())
            {
                foreach (var entity in entities)
                {
                    InternalUpdate(entity);
                }

                await SaveChangesAsync(cancellationToken);
            }
        }

        public virtual async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            InternalDelete(entity);
            await SaveChangesAsync(cancellationToken);
        }

        public virtual async Task DeleteAllAsync(IEnumerable<TEntity> entities,
            CancellationToken cancellationToken = default)
        {
            if (entities.EmptyIfNull().Any())
            {
                foreach (var entity in entities)
                {
                    InternalDelete(entity);
                }

                await SaveChangesAsync(cancellationToken);
            }
        }
        public virtual async Task DeleteAllAsync(Expression<Func<TEntity, bool>> predicate = default,
           CancellationToken cancellationToken = default)
        {
            var querySelect = WithPredicate(Set, predicate);
            if (querySelect.EmptyIfNull().Any())
            {
                foreach (var entity in querySelect)
                {
                    InternalDelete(entity);
                }

                await SaveChangesAsync(cancellationToken);
            }
        }
        protected virtual async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            await Context.SaveChangesAsync(cancellationToken);

        protected virtual void InternalAdd(TEntity entity)
        {
            var user = this.UserAccessor.ProjectUser;
            var (userId, timestamp) = this.GetAuditInfo();

            if (entity is ICreationAuditable creationAudited)
            {
                creationAudited.CreatedById = userId;
                creationAudited.CreatedOn =
                    creationAudited.CreatedOn != default ? creationAudited.CreatedOn : timestamp;
            }

            if (entity is IModificationAuditable modificationAudited)
            {
                modificationAudited.UpdatedById = userId;
                modificationAudited.UpdatedOn = modificationAudited.UpdatedOn != default
                    ? modificationAudited.UpdatedOn
                    : timestamp;
            }

            this.Context.Add(entity);
        }

        protected virtual void InternalUpdate(TEntity entity)
        {
            this.SyncRowVersion(entity);
            if (entity is IModificationAuditable modificationAudited)
            {
                var (userId, timestamp) = this.GetAuditInfo();

                modificationAudited.UpdatedById = userId;
                modificationAudited.UpdatedOn = timestamp;
            }
        }

        protected virtual void InternalDelete(TEntity entity)
        {
            this.SyncRowVersion(entity);
            this.Context.Remove(entity);
        }

        private static IQueryable<TEntity> WithPredicate(IQueryable<TEntity> query, Expression<Func<TEntity, bool>> predicate)
            => predicate != default ? query.Where(predicate) : query;

        private (int? userId, DateTime timestamp) GetAuditInfo() => (this.UserAccessor.ProjectUser?.Id, DateTime.UtcNow);

        private static IQueryable<TEntity> IncludeAllQuery(IQueryable<TEntity> query, Expression<Func<TEntity, object>>[] includes)
            => includes.AsNotNull().Aggregate(query, (current, include) => current.Include(include));

        private void SyncRowVersion(TEntity entity)
        {
            if (entity is IConcurrentEntity concurrentEntity)
            {
                Context.Entry(entity).OriginalValues[nameof(IConcurrentEntity.RowVersion)] =
                    concurrentEntity.RowVersion;
            }
        }
    }
}
