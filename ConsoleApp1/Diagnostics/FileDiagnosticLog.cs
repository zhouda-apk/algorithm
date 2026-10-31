using System;
using System.IO;
using System.Text;
namespace ConsoleApp1.Diagnostics
{
    internal static class FileDiagnosticLog
    {
        internal static void Write(string dataDirectory, Exception error)
        {
            try {
                var directory = Path.Combine(dataDirectory, "logs"); Directory.CreateDirectory(directory);
                File.AppendAllText(Path.Combine(directory, DateTime.UtcNow.ToString("yyyy-MM-dd") + ".log"), DateTime.UtcNow.ToString("o") + " " + error + Environment.NewLine, new UTF8Encoding(false));
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) { System.Diagnostics.Trace.TraceError(error.ToString()); }
        }
    }
}
