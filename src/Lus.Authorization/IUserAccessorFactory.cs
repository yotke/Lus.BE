namespace Lus.Authorization
{
    public interface IUserAccessorFactory
    {
        IUserAccessor CreateUserAccessor(IServiceProvider serviceProvider, IProjectUser projectUser);
    }
}
