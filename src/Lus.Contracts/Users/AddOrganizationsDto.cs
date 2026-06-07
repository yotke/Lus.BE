namespace Lus.Contracts.Users
{
    public class AddOrganizationsToUserDto
    {
        public ICollection<int> OrganizationsId { get; set; }

        public int? UserId { get; set; }
    }
}
