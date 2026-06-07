using Lus.Application.Common;
using Lus.Application.Organizations.Entities;

namespace Lus.Application.Contacts.Entities
{
    public class Contact : EntityBase<int>
    {
        public string IdNumber { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public int? OrganizationId { get; set; }

        public Organization Organization { get; set; }
    }
}
