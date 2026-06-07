using Lus.Application.Common.Extensions;
using Lus.Application.Users.Entities;

namespace Lus.Application.Users.Repositories
{
    public interface IUserLoginAttemptsRepository : IGenericRepository<UserLoginAttempt, int>
    {
    }
}
