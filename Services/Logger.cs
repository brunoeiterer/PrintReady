using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

namespace PrintReady.Services
{
    public static class Logger
    {
        private static readonly string LogPath;

        static Logger()
        {
            var LocalAppDataDirectory = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
            var LogDirectory = Path.Combine(LocalAppDataDirectory, "PrintReady");
            LogPath = Path.Combine(LogDirectory, "PrintReadyLog.txt");

            Directory.CreateDirectory(LogDirectory);
            File.Delete(LogPath);
        }

        public static void Log(string log, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = -1)
        {
            string? fileName = null;
            if (filePath != "")
            {
                fileName = Path.GetFileName(filePath);
            }

            if (fileName != null && lineNumber != -1)
            {
                File.AppendAllText(LogPath, $"{DateTime.Now.ToString("d", DateTimeFormatInfo.InvariantInfo)} {fileName}@{lineNumber} {log}{Environment.NewLine}");
            }
            else
            {
                File.AppendAllText(LogPath, $"{DateTime.Now.ToString("d", DateTimeFormatInfo.InvariantInfo)} {log}{Environment.NewLine}");
            }
        }

        public static string GetLogContent() => File.ReadAllText(LogPath);
    }
}
