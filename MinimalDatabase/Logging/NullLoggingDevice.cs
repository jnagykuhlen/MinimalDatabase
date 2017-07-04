using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinimalDatabase.Logging
{
    public class NullLoggingDevice : ILoggingDevice
    {
        public void WriteLine(string message) { }
        public void WriteLine(string sender, string message) { }
    }
}
