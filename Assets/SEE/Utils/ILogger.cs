using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEE
{
    public interface ILogger
    {
        void LogDebug(string message);
        void LogError(string message);
        void LogException(Exception exception);
        void LogInfo(string message);
    }
}

