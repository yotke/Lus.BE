using Lus.Contracts.Common;

namespace Lus.Application.Common.Interfaces
{
    public interface ICommonLogger
    {
        void Log(object logMessage, LoggerTypeEnum typeOfLog, string source = "");
    }
}
