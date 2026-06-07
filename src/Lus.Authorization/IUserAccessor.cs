namespace Lus.Authorization
{
    public interface IUserAccessor
    {
        IProjectUser ProjectUser { get; }

        IDisposable CreateUserScope(IProjectUser projectUser);
    }
}