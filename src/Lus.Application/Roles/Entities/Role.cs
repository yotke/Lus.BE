using Lus.Application.Common;
using Lus.Application.Organizations.Entities;
using Lus.Application.Users.Entities;

namespace Lus.Application.Roles.Entities
{
    public class Role : EntityBase<int>
    {
        public string Name { get; set; }

        public int Immunity { get; set; }

        public string HebrewName { get; set; }

        public bool ShowToAdmin { get; set; }

        public int? OrganizationId { get; set; }

        public Organization Organization { get; set; }

        public ICollection<UserRole> UserRoles { get; set; }
    }
}
