using Lus.Application.Common.Services;
using Lus.Application.HtmlTemplates.Entities;
using Lus.Authorization;
using Lus.Infrastructure.Persistence;
using Lus.Application.HtmlTemplates.Repositories;

namespace Lus.Infrastructure.Repositories
{
    public class HtmlTemplatesRepository : EntityFrameworkRepository<HtmlTemplate, int>, IHtmlTemplatesRepository
    {
        public HtmlTemplatesRepository(ApplicationContext context, IChangeApplierService changeApplier,
            IUserAccessor userAccessor)
            : base(context, userAccessor)
        {
        }
    }
}
