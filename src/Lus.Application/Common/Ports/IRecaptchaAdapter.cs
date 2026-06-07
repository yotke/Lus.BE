namespace Lus.Application.Common.Ports
{
    public interface IRecaptchaAdapter
    {
        Task<bool> CheckRecaptcha(CancellationToken cancellationToken = default);
    }
}
