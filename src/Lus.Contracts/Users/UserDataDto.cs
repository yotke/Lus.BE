namespace Lus.Contracts.Users
{
    public class UserDataDto
    {
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public string UserName { get; set; }

        public string Phone { get; set; }

        public string IdNumber { get; set; }

        public string PasswordHash { get; set; }

        public bool IsActive { get; set; }

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

        public DateTime CreatedOn { get; set; }

        public int CreatedById { get; set; }

        public DateTime UpdatedOn { get; set; }

        public int UpdatedById { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedOn { get; set; }

        public int? DeletedById { get; set; }
    }
}
