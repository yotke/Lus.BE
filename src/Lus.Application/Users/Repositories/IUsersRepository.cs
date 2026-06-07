using Lus.Application.Common.Extensions;
using Lus.Application.Users.Entities;
using System.Linq.Expressions;

namespace Lus.Application.Users.Repositories
{
    public interface IUsersRepository : IGenericRepository<User, int>
    {
        Task<User> UpdateWithRestrictionsAsync(User user);

        Task<User> FindByUserNameAsync(string userName, Expression<Func<User, ICollection<UserLoginAttempt>>> navigationPropertyPath = null);

        Task<User> UpdateUserAsync(User userToUpdate);

        Task UpdateUserClaimsAsync(int userId, ICollection<KeyValuePair<string, string>> claims);
    }
}
