using System;
using System.IO;
using System.Threading;

namespace _8F.Services
{
    public static class DiagnosticLogger
    {
        private static readonly object _lock = new object();
        private static readonly string _baseDirLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "diagnostic_freeze.log");
        private const string _projectLog = @"D:\New folder\Eddy\EddyFaster\diagnostic_freeze.log";

        public static void Log(string tag, string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] [Thread-{Thread.CurrentThread.ManagedThreadId:D2}] [{tag}] {message}";
            System.Diagnostics.Debug.WriteLine(line);

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_projectLog, line + Environment.NewLine);
                }
                catch { }

                if (_baseDirLog != _projectLog)
                {
                    try
                    {
                        File.AppendAllText(_baseDirLog, line + Environment.NewLine);
                    }
                    catch { }
                }
            }
        }
    }
}
