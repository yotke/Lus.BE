namespace Lus.Application.Common.Exceptions
{
    public class CommonApplicationException : Exception
    {
        public CommonApplicationException(string message, string code, int exceptionId, Exception innerException, double? lockTimeLeft = null)
            : base(message, innerException)
        {
            Code = code;
            ExceptionId = exceptionId;
            LockTimeLeft = lockTimeLeft;
        }

        public CommonApplicationException(string message, int exceptionId, Exception innerException)
            : this(message, null, exceptionId, innerException)
        { }

        public CommonApplicationException(string message, string code, int exceptionId, double? lockTimeLeft = null)
            : this(message, code, exceptionId, innerException: null)
        { }

        public CommonApplicationException(string message, int exceptionId)
            : this(message, exceptionId, innerException: null)
        { }

        public CommonApplicationException(string message, Exception innerException)
            : this(message, 0, innerException)
        { }

        public CommonApplicationException(string message, string code)
            : this(message, code, 0)
        { }

        public CommonApplicationException(string message)
            : this(message, 0)
        { }

        public CommonApplicationException()
            : this(null, 0)
        { }

        public string Code { get; set; }

        public int ExceptionId { get; set; }

        public double? LockTimeLeft { get; set; }
    }
}
