
using Lus.Application.Common;
using Lus.Application.Contacts.Entities;
using Lus.Application.HtmlTemplates.Entities;
using Lus.Application.Images.Entities;
using Lus.Application.ProjectsTemplates.Entities;
using Lus.Application.Roles.Entities;
using Lus.Application.Users.Entities;

namespace Lus.Application.Organizations.Entities
{
    public class Organization : EntityBase<int>
    {
        public string Name { get; set; }

        public int Langitude { get; set; }

        public string Lotitude { get; set; }

        public int AccountingNumber { get; set; }

        public int? MunicipalityRankId { get; set; }        

        public int? GeoRegionId { get; set; }

        public ICollection<UserOrganization>? UserOrganizations { get; set; }

        public ICollection<HtmlTemplate>? HtmlTemplates { get; set; }

        public ICollection<Contact>? Contacts { get; set; }
        public ICollection<ProjectTemplate>? ProjectsTemplates { get; set; }

        public ICollection<Image>? Images { get; set; }

        public ICollection<Role> Roles { get; set; }

    }
}