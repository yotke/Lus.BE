namespace Lus.Contracts.Users.Types
{
    public enum LoginFailReasonType
    {
        UserNotFound,
        WrongPassword,
        UserNotConfirm,
        PasswordExpired,
        BlockedUser
    }
}
