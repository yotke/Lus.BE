using Lus.Application.Common.Services;
using Lus.Application.Users.Entities;
using Lus.Application.Users.Repositories;
using Lus.Authorization;
using Lus.Infrastructure.Persistence;

namespace Lus.Infrastructure.Repositories
{
    public class UserRecruitmentProcessesRepository : EntityFrameworkRepository<UserRecruitmentProcess, int>, IUserRecruitmentProcessesRepository
    {
        public UserRecruitmentProcessesRepository(ApplicationContext context, IChangeApplierService changeApplier,
            IUserAccessor userAccessor)
            : base(context, userAccessor)
        {
        }
    }
}
