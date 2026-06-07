using Lus.Application.Common.Services;
using Lus.Application.Users.Entities;
using Lus.Application.Users.Repositories;
using Lus.Authorization;
using Lus.Infrastructure.Persistence;

namespace Lus.Infrastructure.Repositories
{
    public class UserLoginInfoRepository : EntityFrameworkRepository<UserLoginInfo, int>, IUserLoginInfoRepository
    {
        public UserLoginInfoRepository(ApplicationContext context, IChangeApplierService changeApplier,
            IUserAccessor userAccessor)
            : base(context, userAccessor)
        {
        }
    }
}
