using Lus.Application.Common;
using Lus.Application.Organizations.Entities;

namespace Lus.Application.Users.Entities
{
    public class UserOrganization : EntityBase<int>
    {
        public int? UserId { get; set; }

        public User User { get; set; }

        public int OrganizationId { get; set; }

        public Organization Organization { get; set; }
    }
}
