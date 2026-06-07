using Lus.Application.Common.Services;
using Lus.Application.Organizations.Repositories;
using Lus.Application.Organizations.Entities;
using Lus.Authorization;
using Lus.Infrastructure.Persistence;

namespace Lus.Infrastructure.Repositories
{
    public class OrganizationsRepository : EntityFrameworkRepository<Organization, int>, IOrganizationsRepository
    {
        public OrganizationsRepository(ApplicationContext context, IChangeApplierService changeApplier,
            IUserAccessor userAccessor)
            : base(context, userAccessor)
        {
        }
    }
}
