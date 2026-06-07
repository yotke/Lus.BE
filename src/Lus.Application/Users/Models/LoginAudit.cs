using Lus.Contracts.Users.Types;

namespace Lus.Application.Users.Models
{
    public class LoginAudit
    {
        public int? UserId { get; set; }

        public string UserName { get; set; }

        public LoginAuditType AuditType { get; set; }
    }
}
