using Lus.Application.Common;
using Lus.Application.Organizations.Entities;
using Lus.Application.Users.Entities;
using Lus.Contracts.Images.Types;

namespace Lus.Application.Images.Entities
{
    public class Image : EntityBase<int>
    {
        public string Name { get; set; }

        public string UniqueId { get; set; }

        public ImageType ImageType { get; set; }

        public int? OrganizationId { get; set; }

        public Organization Organization { get; set; }

        public byte[] FileContent { get; set; }

        public int? UserId { get; set; }

        public int? Status { get; set; }

        public User User { get; set; }
    }
}
