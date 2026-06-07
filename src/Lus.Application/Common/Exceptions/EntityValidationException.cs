using Lus.Contracts;

namespace Lus.Application.Common.Exceptions
{
    public class EntityValidationException : CommonApplicationException
    {
        private const string ErrorMessage = "Validation error";

        public EntityValidationException(string filedName, string fieldError, int exceptionId)
            : base(fieldError, ErrorCodes.EntityInvalid, exceptionId)
        {
        }

        public EntityValidationException(string filedName, int exceptionId)
            : base(ErrorMessage, ErrorCodes.EntityInvalid, exceptionId)
        {
        }

        public EntityValidationException()
            : base(ErrorMessage, ErrorCodes.EntityInvalid, 0)
        {
        }
    }
}
