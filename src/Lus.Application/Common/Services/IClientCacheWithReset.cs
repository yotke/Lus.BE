namespace Lus.Application.Common.Services
{
    public interface IClientCacheWithReset
    {
        Task RemoveAsync(string key);
    }
}
