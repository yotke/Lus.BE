namespace Lus.Authorization
{
    public interface IProjectUser
    {
        int Id { get; }

        string Name { get; }

        int? OrganizationId { get; }

        IEnumerable<string> Roles { get; }

        IEnumerable<(string, string)> Claims { get; }
    }
}
