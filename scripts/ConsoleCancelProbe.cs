// Validation helper only. It creates its own console so CTRL_C_EVENT cannot reach the user's console.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

internal static class ConsoleCancelProbe
{
    private delegate bool Handler(uint kind);
    [DllImport("kernel32.dll")] private static extern bool FreeConsole();
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool AllocConsole();
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetConsoleCtrlHandler(Handler handler, bool add);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GenerateConsoleCtrlEvent(uint kind, uint group);
    private static readonly Handler KeepHostAlive = kind => kind == 0;

    private static int Main(string[] args)
    {
        if (args.Length != 3) return 2;
        var lines = new List<string>(); int signalled = 0; bool sent = false;
        try
        {
            FreeConsole();
            if (!AllocConsole() || !SetConsoleCtrlHandler(KeepHostAlive, true)) throw new InvalidOperationException("Cannot allocate isolated console.");
            using (var child = new Process())
            {
                child.StartInfo = new ProcessStartInfo(args[0], "--data-path \"" + args[1] + "\"")
                {
                    UseShellExecute = false, CreateNoWindow = false, RedirectStandardInput = true,
                    RedirectStandardOutput = true, RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
                };
                child.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data == null) return;
                    lock (lines) lines.Add(e.Data);
                    if (e.Data.Contains("階段：Warmup") && Interlocked.Exchange(ref signalled, 1) == 0)
                        sent = GenerateConsoleCtrlEvent(0, 0);
                };
                child.ErrorDataReceived += (sender, e) => { if (e.Data != null) lock (lines) lines.Add("STDERR " + e.Data); };
                child.Start(); child.BeginOutputReadLine(); child.BeginErrorReadLine();
                var commands = "1\n4\n取消驗收\n1\n3\n5000\n-10000\n10000\n42\nY\n0\n2\n1\n1\nY\n0\n";
                var bytes = new UTF8Encoding(false).GetBytes(commands);
                child.StandardInput.BaseStream.Write(bytes, 0, bytes.Length); child.StandardInput.Close();
                if (!child.WaitForExit(30000)) { child.Kill(); child.WaitForExit(); throw new TimeoutException("Console probe timed out."); }
                child.WaitForExit();
                lock (lines) File.WriteAllLines(args[2], lines, new UTF8Encoding(false));
                return sent && child.ExitCode == 0 ? 0 : 1;
            }
        }
        catch (Exception e)
        {
            lock (lines) { lines.Add(e.ToString()); File.WriteAllLines(args[2], lines, new UTF8Encoding(false)); }
            return 1;
        }
        finally { FreeConsole(); }
    }
}
