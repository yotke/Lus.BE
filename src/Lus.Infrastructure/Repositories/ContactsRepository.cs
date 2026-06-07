using Lus.Application.Common.Services;
using Lus.Application.Contacts.Entities;
using Lus.Application.Contacts.Repositories;
using Lus.Authorization;
using Lus.Infrastructure.Persistence;

namespace Lus.Infrastructure.Repositories
{
    public class ContactsRepository : EntityFrameworkRepository<Contact, int>, IContactsRepository
    {
        public ContactsRepository(ApplicationContext context, IChangeApplierService changeApplier,
            IUserAccessor userAccessor)
            : base(context, userAccessor)
        {
        }
    }
}
