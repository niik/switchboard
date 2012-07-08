using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Switchboard.ConsoleHost.Logging
{
    /// <summary>
    /// Prints debug messages straight to the console. Will color a message
    /// based on IP-address and port if it contains one.
    /// </summary>
    public class ConsoleLogger : TraceListener
    {
        private readonly object syncRoot = new object();
        private static Regex ipPortRe = new Regex(@"(?:[0-9]{1,3}\.){3}[0-9]{1,3}:\d{1,5}");

        public override void Write(string message)
        {
            lock (syncRoot)
                System.Console.Write(message);
        }

        public override void WriteLine(string message)
        {
            var m = ipPortRe.Match(message);

            if (m.Success)
            {
                uint h = (uint)m.Value.GetHashCode();
                ConsoleColor c;

                do
                {
                    c = (ConsoleColor)(h % 16);
                    h++;
                } while (c == ConsoleColor.Black || c == ConsoleColor.Gray);

                lock (syncRoot)
                {
                    var current = System.Console.ForegroundColor;
                    Console.ForegroundColor = c;
                    Console.WriteLine(message);
                    Console.ForegroundColor = current;
                }

                return;
            }

            lock (syncRoot)
            {
                System.Console.WriteLine(message);
            }
        }
    }
}
