using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Lus.Application.Common.Services;
using Lus.Application.Users.Models;
using Lus.Contracts.Users.Types;
using Lus.Infrastructure.Extensions;
using Lus.Infrastructure.Options;

namespace Lus.Infrastructure.Services
{
    public class UserLoginAttemptsAuditService : IUserLoginAttemptsAuditService
    {
        private const string KeyPrefix = "UserLoginAttempt_";
        private readonly ILogger<UserLoginAttemptsAuditService> logger;
        private readonly IDistributedCache distributedCache;
        private readonly UserLoginAuditingOptions options;
        private readonly SemaphoreSlim locker = new(1);

        public UserLoginAttemptsAuditService(ILogger<UserLoginAttemptsAuditService> logger,
            IDistributedCache distributedCache,
            IOptions<UserLoginAuditingOptions> options)
        {
            this.logger = logger;
            this.distributedCache = distributedCache;
            this.options = options.Value;
        }

        public Task<LoginAudit> GetUserLoginFailureEventAsync(string userName, LoginFailReasonType failReason, int? userId) =>
            ExecuteSafeAsync(() =>
            {
                var failure = failReason switch
                {
                    LoginFailReasonType.BlockedUser => Task.FromResult(CreateLoginAudit(userName, userId,
                        LoginAuditType.LoginDisabledAccount)),
                    LoginFailReasonType.UserNotFound => Task.FromResult(CreateLoginAudit(userName, userId,
                        LoginAuditType.LoginNotExistingAccount)),
                    LoginFailReasonType.WrongPassword => PrepareWrongPasswordAttemptAsync(userName, userId),
                    _ => throw new InvalidOperationException(
                        $"Unsupported fail reason {failReason} for user {userName}")
                };

                return failure;
            });

        public Task<LoginAudit> GetUserLoginAfterFailureEventAsync(string userName, int userId) =>
            ExecuteSafeAsync(() =>
            {
                var failure = PrepareUserLoginAfterFailureEventAsync(userName, userId,
                    this.options.UserAttempts);

                return failure;
            });

        private Task<LoginAudit> PrepareUserLoginAfterFailureEventAsync(string userName, int userId,
            int attemptsCount) =>
            ExecuteWithLockAsync(async () =>
            {
                var items = await GetItemListAsync(KeyFor(userName));
                await ClearItemListAsync(KeyFor(userName));
                if (items.Count >= attemptsCount)
                {
                    return CreateLoginAudit(userName, userId, LoginAuditType.LoginAfterLoginFail);
                }

                return null;
            });

        private async Task<List<LoginOccurrenceDescription>> GetItemListAsync(string key)
        {
            var items = await this.distributedCache.GetModelByKeyAsync<List<LoginOccurrenceDescription>>(key);

            return items ?? new List<LoginOccurrenceDescription>();

        }

        private Task ClearItemListAsync(string key)
            => this.distributedCache.RemoveAsync(key);

        private Task<LoginAudit> PrepareWrongPasswordAttemptAsync(string userName, int? userId) =>
            CreateWrongPasswordAttemptAsync(userName, userId, this.options.UserIntervalSec,
                this.options.UserAttempts);

        private async Task<LoginAudit> CreateWrongPasswordAttemptAsync(string userName, int? userId,
            int interval, int attempts)
        {
            var items = await RenewOccurrenceAsync(userName, interval);
            if (items.Count >= attempts)
            {
                return CreateLoginAudit(userName, userId, LoginAuditType.LoginFail);
            }

            return null;
        }

        private Task<List<LoginOccurrenceDescription>> RenewOccurrenceAsync(string userName, int intervalSec) =>
            ExecuteWithLockAsync(async () =>
            {
                var items = await GetItemListAsync(KeyFor(userName));

                items.Add(LoginOccurrenceDescription.NewInstance);
                var validItems = items
                    .Where(i => i.GetOccurrence() >= DateTime.UtcNow.AddSeconds(-1 * intervalSec)).ToList();

                await this.distributedCache.StoreModelAsync(KeyFor(userName), validItems, this.options.KeepAttemptsIntervalMin);

                return validItems;
            });

        private async Task<TResult> ExecuteSafeAsync<TResult>(Func<Task<TResult>> action)
        {
            try
            {
                return await action();
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception, "Was not able to process login attempt due to error:");
            }

            return default;
        }

        private async Task<TResult> ExecuteWithLockAsync<TResult>(Func<Task<TResult>> action)
        {
            try
            {
                await this.locker.WaitAsync();
                return await action();
            }
            finally
            {
                this.locker.Release();
            }
        }

        private static string KeyFor(string userName) => $"{KeyPrefix}{userName.ToLower()}";

        private static LoginAudit CreateLoginAudit(
            string userName,
            int? userId,
            LoginAuditType loginAudit)
            => new() { UserName = userName, AuditType = loginAudit, UserId = userId };

        private class LoginOccurrenceDescription
        {
            public long OccurredOn { get; init; }

            public DateTime GetOccurrence() => DateTime.FromBinary(OccurredOn);

            public static LoginOccurrenceDescription NewInstance
                => new() { OccurredOn = DateTime.UtcNow.ToBinary() };
        }
    }
}