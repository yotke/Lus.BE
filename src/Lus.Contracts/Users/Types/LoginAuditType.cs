namespace Lus.Contracts.Users.Types
{
    public enum LoginAuditType
    {
        LoginFail,
        LoginAfterLoginFail,
        LoginDisabledAccount,
        LoginNotExistingAccount
    }
}
