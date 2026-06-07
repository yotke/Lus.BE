using Lus.Application.Common.Services;
using Lus.Application.ProjectsTemplates.Entities;
using Lus.Application.ProjectsTemplates.Repositories;
using Lus.Authorization;
using Lus.Infrastructure.Persistence;

namespace Lus.Infrastructure.Repositories
{
    public class ProjectsTemplatesRepository : EntityFrameworkRepository<ProjectTemplate, int>, IProjectsTemplatesRepository
    {
        public ProjectsTemplatesRepository(ApplicationContext context, IChangeApplierService changeApplier,
            IUserAccessor userAccessor)
            : base(context, userAccessor)
        {
        }
    }
}
