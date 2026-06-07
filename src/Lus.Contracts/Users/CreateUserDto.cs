namespace Lus.Contracts.Users
{
    public class CreateUserDto
    {
        public string Email { get; set; }

        public string ConfirmEmail { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Password { get; set; }

        public string ConfirmPassword { get; set; }

        public string Phone { get; set; }

        public string IdNumber { get; set; }
    }
}
