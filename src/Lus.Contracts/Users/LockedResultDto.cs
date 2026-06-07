namespace Lus.Contracts.Users
{
    public class LockedResultDto
    {
        public bool IsLocked { get; set; }

        public double? LockTimeLeft { get; set; }

        public bool IsUserFound { get; set; }
    }
}
