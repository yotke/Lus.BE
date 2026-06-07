using Lus.Application.Common.Exceptions;
using Lus.Contracts;

namespace Lus.Infrastructure.Exceptions
{
    public class MembershipException : CommonApplicationException
    {
        public MembershipException(int exceptionId)
            : base(null, ErrorCodes.InvalidToken, exceptionId)
        {
        }

        public MembershipException(int exceptionId, double? lockTimeLeft)
            : base(null, ErrorCodes.InvalidToken, exceptionId, lockTimeLeft)
        {
        }

        public MembershipException(string message)
            : this(message, 0)
        {
        }

        public MembershipException(string message, int exceptionId)
            : base(message, ErrorCodes.InvalidToken, exceptionId)
        {
        }
    }
}
