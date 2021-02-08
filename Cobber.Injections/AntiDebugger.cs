using System;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;

static class AntiDebugger
{
    [DllImport("ntdll.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
    static extern int NtQueryInformationProcess(IntPtr ProcessHandle, int ProcessInformationClass,
        byte[] ProcessInformation, uint ProcessInformationLength, out int ReturnLength);
    [DllImport("ntdll.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
    static extern uint NtSetInformationProcess(IntPtr ProcessHandle, int ProcessInformationClass,
        byte[] ProcessInformation, uint ProcessInformationLength);
    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr hObject);
    [DllImport("kernel32.dll")]
    static extern bool IsDebuggerPresent();
    [DllImport("kernel32.dll")]
    static extern int OutputDebugString(string str);

    public static void Initialize()
    {
        if (Environment.GetEnvironmentVariable("COR_ENABLE_PROFILING") != null ||
            Environment.GetEnvironmentVariable("COR_PROFILER") != null)
            Environment.FailFast("Profiler detected");

        Thread thread = new Thread(AntiDebug);
        thread.IsBackground = true;
        thread.Start(null);
    }
    static void AntiDebug(object thread)
    {
        Thread th = thread as Thread;
        if (th == null)
        {
            th = new Thread(AntiDebug);
            th.IsBackground = true;
            th.Start(Thread.CurrentThread);
            Thread.Sleep(500);
        }
        while (true)
        {
            //Managed
            if (Debugger.IsAttached || Debugger.IsLogging())
                Environment.FailFast("Debugger detected (Managed)");

            //IsDebuggerPresent
            if (IsDebuggerPresent())
                Environment.FailFast("=_=");

            //Open process
            IntPtr ps = Process.GetCurrentProcess().Handle;
            if (ps == IntPtr.Zero)
                Environment.FailFast("Cannot open process");

            //OutputDebugString
            if (OutputDebugString("=_=") > IntPtr.Size)
                Environment.FailFast("Debugger detected");

            //Close
            try
            {
                CloseHandle(IntPtr.Zero);
            }
            catch
            {
                Environment.FailFast("Debugger detected");
            }

            if (!th.IsAlive)
                Environment.FailFast("Loop broken");

            Thread.Sleep(1000);
        }
    }

    public static void InitializeSafe()
    {
        if (Environment.GetEnvironmentVariable("COR_ENABLE_PROFILING") != null ||
            Environment.GetEnvironmentVariable("COR_PROFILER") != null)
            Environment.FailFast("Profiler detected");

        Thread thread = new Thread(AntiDebugSafe);
        thread.IsBackground = true;
        thread.Start(null);
    }
    private static void AntiDebugSafe(object thread)
    {
        Thread th = thread as Thread;
        if (th == null)
        {
            th = new Thread(AntiDebugSafe);
            th.IsBackground = true;
            th.Start(Thread.CurrentThread);
            Thread.Sleep(500);
        }
        while (true)
        {
            if (Debugger.IsAttached || Debugger.IsLogging())
                Environment.FailFast("Debugger detected (Managed)");

            if (!th.IsAlive)
                Environment.FailFast("Loop broken");

            Thread.Sleep(1000);
        }
    }
}
