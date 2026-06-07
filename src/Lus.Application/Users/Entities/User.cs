using Lus.Application.Common;
using Lus.Application.Images.Entities;
using Lus.Application.Notifications.Entities;

namespace Lus.Application.Users.Entities
{
    public class User : EntityBase<int>
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public string UserName { get; set; }

        public string Phone { get; set; }

        public string IdNumber { get; set; }

        public string PasswordHash { get; set; }

        public bool IsConfirmed { get; set; }

        public string ConfirmationToken { get; set; }

        public DateTime? LastPasswordFailureDate { get; set; }

        public DateTime? PasswordChangedDate { get; set; }

        public string PasswordVerificationToken { get; set; }

        public string SmsVerificationToken { get; set; }

        public DateTime? VerificationTokenExpiration { get; set; }

        public DateTime? SmsTokenExpiration { get; set; }

        public ICollection<string> ClientSecrets { get; set; }

        public ICollection<string> AllowedScopes { get; set; }

        public ICollection<string> AllowedGrantTypes { get; set; }

        public ICollection<KeyValuePair<string, string>> Claims { get; set; }

        public ICollection<UserLoginAttempt> UserLoginAttempts { get; set; }

        public ICollection<UserPasswordHistory> UserPasswordHistories { get; set; }

        public ICollection<MailNotification> MailNotifications { get; set; }

        public ICollection<SmsNotification> SmsNotifications { get; set; }


        public ICollection<UserOrganization>? UserOrganizations { get; set; }

        public ICollection<UserRecruitmentProcess>? UserRecruitmentProcesses { get; set; }

        public ICollection<UserRole>? UserRoles { get; set; }

        public ICollection<Image> Images { get; set; }

        public ICollection<AppliedUserTender> AppliedUserTenders { get; set; }


        public ICollection<FavoriteUserTender> FavoriteUserTenders { get; set; }

        public ICollection<UserLoginInfo> UserLoginInfos { get; set; }
    }
}
