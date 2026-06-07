using Microsoft.Extensions.Options;
using Lus.Application.Common.Services;
using Lus.Infrastructure.Options;

namespace Lus.Infrastructure.Services
{
    public class ChangeApplierService : IChangeApplierService
    {
        private readonly UpdateEntitiesOptions updateOptions;

        public ChangeApplierService(IOptions<UpdateEntitiesOptions> updateOptions) => this.updateOptions = updateOptions.Value;

        public bool SetUpdates<TEntity>(TEntity storedEntity, TEntity updatedEntity)
            where TEntity : class, new()
        {
            var isUpdate = false;
            foreach (var updateFunction in this.updateOptions.UpdateFunctions<TEntity>())
            {
                var updated = updateFunction(storedEntity, updatedEntity);
                isUpdate = isUpdate || updated;
            }

            return isUpdate;
        }
    }
}
