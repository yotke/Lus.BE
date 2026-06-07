using Lus.Application.Common.Services;
using Lus.Application.Users.Entities;
using Lus.Application.Users.Repositories;
using Lus.Authorization;
using Lus.Infrastructure.Persistence;

namespace Lus.Infrastructure.Repositories
{
    public class UserPasswordHistoriesRepository : EntityFrameworkRepository<UserPasswordHistory, int>, IUserPasswordHistoriesRepository
    {
        public UserPasswordHistoriesRepository(ApplicationContext context, IChangeApplierService changeApplier,
            IUserAccessor userAccessor)
            : base(context, userAccessor)
        {
        }
    }
}
