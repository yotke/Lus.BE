using Microsoft.Extensions.Options;

namespace Lus.Infrastructure.Options
{
    public class ApiCacheOptions : IOptions<ApiCacheOptions>
    {
        public int SlidingExpirationDays { get; set; }

        public ApiCacheOptions Value => this;
    }
}
