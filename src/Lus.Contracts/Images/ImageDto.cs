using Lus.Contracts.Images.Types;

namespace Lus.Contracts.Images
{
    public class ImageDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public ImageType ImageType { get; set; }

        public int? OrganizationId { get; set; }

        public byte[] FileContent { get; set; }

        public int? Status { get; set; }

        public int? UserId { get; set; }
    }
}
