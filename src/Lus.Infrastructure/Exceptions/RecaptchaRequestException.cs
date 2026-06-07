namespace Lus.Infrastructure.Exceptions
{
    public class RecaptchaRequestException : Exception
    {
        public RecaptchaRequestException()
        {
        }
        public RecaptchaRequestException(string message)
            : base(message)
        {
        }
        public RecaptchaRequestException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
