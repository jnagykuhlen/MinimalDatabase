using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinimalDatabase.Logging
{
    public interface ILoggingDevice
    {
        void WriteLine(string message);
        void WriteLine(string sender, string message);
    }
}
