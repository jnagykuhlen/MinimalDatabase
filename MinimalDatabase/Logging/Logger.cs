using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;


namespace MinimalDatabase.Logging
{
    public static class Logger
    {
        private static ILoggingDevice loggingDevice;

        static Logger()
        {
            loggingDevice = new NullLoggingDevice();
        }

        public static void WriteLine(string message)
        {
            loggingDevice.WriteLine(message);
        }

        public static void WriteLine(string sender, string message)
        {
            loggingDevice.WriteLine(sender, message);
        }

        public static void WriteLine(string sender, string format, params object[] arguments)
        {
            loggingDevice.WriteLine(sender, String.Format(format, arguments));
        }
        
        public static ILoggingDevice LoggingDevice
        {
            get
            {
                return loggingDevice;
            }
            set
            {
                loggingDevice = value ?? new NullLoggingDevice();
            }
        }
    }
}
