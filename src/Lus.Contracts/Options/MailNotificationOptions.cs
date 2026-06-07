using System.Net.Mail;

namespace Lus.Contracts.Options
{
    public class MailNotificationOptions
    {
        public string DebugEmails { get; set; }

        public string DebugCalendarEmails { get; set; }

        public string SiteDomain { get; set; }

        public string DisplayName { get; set; }

        public string EmailFrom { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public SmtpDeliveryMethod DeliveryMethod { get; set; }

        public bool EnableSsl { get; set; }

        public int Timeout { get; set; }

        public bool DefaultCredentials { get; set; }
    }
}
