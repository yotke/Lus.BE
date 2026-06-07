using Lus.Contracts;

namespace Lus.Application.Common.Exceptions
{
    public class EntityNotFoundException : CommonApplicationException
    {
        private const string MessageTemplate = "{0} with key {1} not found";

        public EntityNotFoundException(string entityType, int entityKey, int exceptionId)
            : base(string.Format(MessageTemplate, entityType, entityKey), ErrorCodes.EntityNotFound, exceptionId)
        {
        }
      
        public EntityNotFoundException(string message)
            : this(message, 0)
        {
        }

        public EntityNotFoundException(string message, int exceptionId)
            : base(message, ErrorCodes.EntityNotFound, exceptionId)
        {
        }
    }
}
