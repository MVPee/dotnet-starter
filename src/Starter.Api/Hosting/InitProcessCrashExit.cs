namespace Starter.Api.Hosting;

internal static class InitProcessCrashExit
{
    private const int InitProcessId = 1;
    private const int CrashExitCode = 1;

    // As container PID 1 the runtime's abort() is ignored, so a crash would leave the container "running".
    public static void Register()
    {
        if (Environment.ProcessId != InitProcessId)
        {
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += (_, unhandledExceptionEventArgs) =>
        {
            Console.Error.WriteLine(unhandledExceptionEventArgs.ExceptionObject);
            Environment.Exit(CrashExitCode);
        };
    }
}
