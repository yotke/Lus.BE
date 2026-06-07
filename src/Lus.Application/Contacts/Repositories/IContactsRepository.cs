using Lus.Application.Common.Extensions;
using Lus.Application.Contacts.Entities;

namespace Lus.Application.Contacts.Repositories
{
    public interface IContactsRepository : IGenericRepository<Contact, int>
    {
    }
}
