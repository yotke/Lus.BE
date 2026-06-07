using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lus.Application.Common.Services
{
    public interface IChangeApplierService
    {
        bool SetUpdates<TEntity>(TEntity storedEntity, TEntity updatedEntity)
            where TEntity : class, new();
    }
}
