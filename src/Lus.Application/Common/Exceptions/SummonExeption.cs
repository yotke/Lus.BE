using Lus.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lus.Application.Common.Exceptions
{
    public class SummonExeption : CommonApplicationException
    {

        private const string MessageTemplate = "{0} with key {1} not found";

        public SummonExeption(int exceptionId)
     : base(null, ErrorCodes.UpdateSummonFailed, exceptionId)
        {
        }
        public SummonExeption(string entityType, int entityKey, int exceptionId)
     : base(string.Format(MessageTemplate, entityType, entityKey), ErrorCodes.EntityUpdateFailed, exceptionId)
        {
        }
        public SummonExeption(string message)
       : this(message, 0)
        {
        }
        public SummonExeption(string message, int exceptionId)
     : base(message, ErrorCodes.UpdateSummonFailed, exceptionId)
        {
        }
    }
}
