namespace Lus.Application.Common.Options
{
    public class PasswordConfigOptions
    {
        public int PasswordExpiresMaxDays { get; set; }

        public int MaxInvalidPasswordAttempts { get; set; }

        public double UserLockInterval { get; set; }

        public int MinRequiredPasswordLength { get; set; }

        public int MinRequiredNonAlphanumericCharacters { get; set; }

        public int MinRequiredNumericCharacters { get; set; }

        public int MinRequiredSmallLetter { get; set; }

        public int MinRequiredCapitalLetter { get; set; }
    }
}