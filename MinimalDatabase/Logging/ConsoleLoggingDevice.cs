using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MinimalDatabase.Logging
{
    public class ConsoleLoggingDevice : ILoggingDevice
    {
        public void WriteLine(string message)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(message);
        }

        public void WriteLine(string sender, string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(sender);
            Console.Write(": ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(message);
        }
    }
}
