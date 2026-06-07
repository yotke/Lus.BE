using Lus.Application.Common.Services;
using Lus.Application.Notifications.Entities;
using Lus.Application.Notifications.Repositories;
using Lus.Authorization;
using Lus.Infrastructure.Persistence;
using Lus.Infrastructure.Repositories;

namespace Lus.Infrastructure.Repositories
{
    public class MailNotificationsRepository : EntityFrameworkRepository<MailNotification, int>, IMailNotificationsRepository
    {
        public MailNotificationsRepository(ApplicationContext context, IChangeApplierService changeApplier,
            IUserAccessor userAccessor)
            : base(context, userAccessor)
        {
        }
    }
}
