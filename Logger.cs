using System.Threading.Tasks;
using Discord;
using System;

namespace JifBot
{
    public class Logger
    {
        public Task WriteInfo(string message, string source)
        {
            LogMessage msg = new LogMessage(LogSeverity.Info, source, message);
            WriteLog(msg);
            return Task.CompletedTask;
        }

        public Task WriteError(string message, string source, Exception exception)
        {
            LogMessage msg = new LogMessage(LogSeverity.Error, source, message, exception);
            WriteLog(msg);
            return Task.CompletedTask;
        }

        public Task WriteLog(LogMessage lmsg)
        {
            var cc = Console.ForegroundColor;
            switch (lmsg.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogSeverity.Verbose:
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }

            Console.WriteLine($"{DateTime.Now} [{lmsg.Severity,8}] {lmsg.Source}: {lmsg.Message}");
            if (lmsg.Exception != null)
            {
                Console.WriteLine($" >> {lmsg.Exception.Message}");
                if (lmsg.Exception.InnerException != null)
                {
                    Console.WriteLine($"   >> {lmsg.Exception.InnerException.Message}");
                    if (lmsg.Exception.InnerException.InnerException != null)
                        Console.WriteLine($"     >> {lmsg.Exception.InnerException.InnerException.Message}");
                }
            }
            Console.ForegroundColor = cc;
            return Task.CompletedTask;
        }
    }
}
