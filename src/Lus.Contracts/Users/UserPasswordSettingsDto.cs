namespace Lus.Contracts.Users
{
    public class UserPasswordSettingsDto
    {
        public int PasswordLength { get; set; }

        public int NonAlphanumericCharacters { get; set; }

        public int NumericCharacters { get; set; }

        public int SmallLetter { get; set; }

        public int CapitalLetter { get; set; }
    }
}
