namespace Lus.Contracts.Users
{
    public class UserPasswordHistoryDto
    {
        public int Id { get; set; }

        public string PasswordHash { get; set; }

        public int? UserId { get; set; }
    }
}
