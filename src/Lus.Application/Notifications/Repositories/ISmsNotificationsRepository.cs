using Lus.Application.Common.Extensions;
using Lus.Application.Notifications.Entities;

namespace Lus.Application.Notifications.Repositories
{
    public interface ISmsNotificationsRepository : IGenericRepository<SmsNotification, int>
    {
    }
}
