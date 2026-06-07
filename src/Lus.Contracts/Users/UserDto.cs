namespace Lus.Contracts.Users
{
    public class UserDto
    {
        public int Id { get; set; }

        public string FirstName { get; set; }

        public string? TenderName { get; set; } = null;

        public string? OrganizationName { get; set; } = null;

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
    }
}
