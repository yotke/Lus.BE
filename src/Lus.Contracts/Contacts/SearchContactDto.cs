namespace Lus.Contracts.Contacts
{
    /// <summary>
    /// Search DTO for contacts. Property names define what callers may filter/sort on
    /// (the filter engine maps these names onto <c>ContactProjection</c>).
    /// <see cref="OrganizationId"/> is used by the search handler to enforce tenant scoping.
    /// </summary>
    public class SearchContactDto
    {
        public int? Id { get; set; }

        public string? IdNumber { get; set; }

        public string? Name { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public bool? Active { get; set; }

        public int? OrganizationId { get; set; }
    }
}
