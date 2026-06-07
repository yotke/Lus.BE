namespace Lus.Contracts
{
    public class ErrorModel
    {
        public ErrorModel() : this(null)
        {
        }

        public ErrorModel(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public ErrorModel(string message)
        {
            Message = message;
        }

        public ErrorModel(string message, int exceptionId)
        {
            Message = message;
            ExceptionId = exceptionId;
        }

        public ErrorModel(string code, string message, int exceptionId, double? lockTimeLeft = null)
        {
            Code = code;
            Message = message;
            ExceptionId = exceptionId;
            LockTimeLeft = lockTimeLeft;
        }

        public string Code { get; set; }

        public string Message { get; set; }

        public int ExceptionId { get; set; }

        public double? LockTimeLeft { get; set; }
    }
}
