using Lus.Application.Contacts.Entities;
using Lus.Application.Contacts.Projections;
using Lus.FilterEngine.EntityFrameworkCore;
using Lus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lus.Infrastructure.Retrievers
{
    /// <summary>
    /// Builds the base <see cref="IQueryable{ContactProjection}"/> the filter engine
    /// applies predicates/sorting/paging onto. Read-only (AsNoTracking).
    /// </summary>
    public class ContactRetriever : DataRetriever<ContactProjection>
    {
        public ContactRetriever(ApplicationContext context) : base(context)
        {
        }

        protected override IQueryable<ContactProjection> CreateRetrieveQuery(DbContext context) =>
            context.Set<Contact>()
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .Select(c => new ContactProjection
                {
                    Id = c.Id,
                    IdNumber = c.IdNumber,
                    Name = c.Name,
                    Email = c.Email,
                    Phone = c.Phone,
                    Active = c.Active,
                    OrganizationId = c.OrganizationId
                });
    }
}
