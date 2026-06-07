namespace Lus.Application.Contacts.Projections
{
    /// <summary>
    /// Read-model projection for <see cref="Lus.Application.Contacts.Entities.Contact"/>.
    /// Shaped at the DB level by <c>ContactRetriever</c> and mapped to search DTOs.
    /// Includes <see cref="OrganizationId"/> so the search pipeline can org-scope results.
    /// </summary>
    public class ContactProjection
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
