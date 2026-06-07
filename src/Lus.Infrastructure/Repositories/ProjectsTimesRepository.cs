using Lus.Application.Common.Services;
using Lus.Application.ProjectsTimes.Entities;
using Lus.Application.ProjectsTimes.Repositories;
using Lus.Authorization;
using Lus.Infrastructure.Persistence;

namespace Lus.Infrastructure.Repositories
{
    public class ProjectsTimesRepository : EntityFrameworkRepository<ProjectTime, int>, IProjectsTimesRepository
    {
        public ProjectsTimesRepository(ApplicationContext context, IChangeApplierService changeApplier,
            IUserAccessor userAccessor)
            : base(context, userAccessor)
        {
        }
    }
}