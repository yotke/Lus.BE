namespace Lus.Authorization.Models
{
    public class ProjectUser : IProjectUser
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int? OrganizationId { get; set; }

        public IEnumerable<string> Roles { get; set; } = new List<string>();

        public IEnumerable<(string, string)> Claims { get; set; } = new List<(string, string)>();
    }
}
