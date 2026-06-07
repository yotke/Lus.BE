using Microsoft.Extensions.Options;

namespace Lus.Infrastructure.Options
{
    public class UserLoginAuditingOptions : IOptions<UserLoginAuditingOptions>
    {
        public int UserIntervalSec { get; set; }

        public int UserAttempts { get; set; }

        public int KeepAttemptsIntervalMin { get; set; }

        public UserLoginAuditingOptions Value => this;
    }
}
