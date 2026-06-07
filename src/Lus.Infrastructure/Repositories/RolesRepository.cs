using Lus.Application.Common.Services;
using Lus.Application.Roles.Entities;
using Lus.Application.Roles.Repositories;
using Lus.Authorization;
using Lus.Infrastructure.Persistence;

namespace Lus.Infrastructure.Repositories
{
    public class RolesRepository : EntityFrameworkRepository<Role, int>, IRolesRepository
    {
        private readonly string spCreateUpdateMuniRoles = "spCreateUpdateMuniRoles";

        public RolesRepository(ApplicationContext context, IChangeApplierService changeApplier,
            IUserAccessor userAccessor)
            : base(context, userAccessor)
        {
        }

        public async Task RunSpCreateUpdateMuniRoles()
        {
            await RunStoredProcedureWithoutParameters(spCreateUpdateMuniRoles);
        }
    }
}
