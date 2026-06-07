using Lus.Application.Common.Extensions;
using Lus.Application.Roles.Entities;

namespace Lus.Application.Roles.Repositories
{
    public interface IRolesRepository : IGenericRepository<Role, int>
    {
        Task RunSpCreateUpdateMuniRoles();
    }
}
