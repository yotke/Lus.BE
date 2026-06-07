using Microsoft.EntityFrameworkCore;
using Lus.Application.Common.Services;
using Lus.Application.Users.Entities;
using Lus.Application.Users.Repositories;
using Lus.Authorization;
using Lus.Infrastructure.Persistence;
using System.Linq.Expressions;

namespace Lus.Infrastructure.Repositories
{
    public class UsersRepository : EntityFrameworkRepository<User, int>, IUsersRepository
    {
        private readonly IChangeApplierService changeApplier;

        public UsersRepository(ApplicationContext context, IChangeApplierService changeApplier, IUserAccessor userAccessor)
            : base(context, userAccessor) =>
            this.changeApplier = changeApplier;

        public async Task<User> UpdateWithRestrictionsAsync(User user)
        {
            var storedUser = await Context.Set<User>().FindAsync(user.Id);
            return this.changeApplier.SetUpdates(storedUser, user) ? await UpdateAsync(storedUser) : user;
        }

        public async Task<User> FindByUserNameAsync(string userName, Expression<Func<User, ICollection<UserLoginAttempt>>> navigationPropertyPath = null) =>
            navigationPropertyPath == null
                ? await Set
                    .Where(a => a.UserName == userName)
                    .FirstOrDefaultAsync()
                : await Set
                    .Where(a => a.UserName == userName)
                    .Include(navigationPropertyPath)
                    .FirstOrDefaultAsync();

        public async Task<User> UpdateUserAsync(User userToUpdate)
        {
            Context.Entry(userToUpdate).State = EntityState.Modified;
            await Context.SaveChangesAsync();
            return userToUpdate;
        }

        public async Task UpdateUserClaimsAsync(int userId, ICollection<KeyValuePair<string, string>> claims)
        {
            var user = await Context.Set<User>().FindAsync(userId);
            if (user != null)
            {
                user.Claims = claims;
                await Context.SaveChangesAsync();
            }
        }
    }
}
