using System.Runtime.Versioning;

namespace CatLocker;

internal static class Program
{
    [STAThread]
    [SupportedOSPlatform("windows10.0.17763")]
    private static void Main()
    {
        const string mutexName = @"Local\CatLocker_SingleInstance";
        using Mutex mutex = new(initiallyOwned: false, mutexName, out _);
        bool acquired = false;
        try
        {
            try
            {
                acquired = mutex.WaitOne(0, exitContext: false);
            }
            catch (AbandonedMutexException)
            {
                // Previous instance crashed without releasing the mutex.
                // We now own it — safe to continue.
                acquired = true;
            }

            if (!acquired)
            {
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new CatLockerApplicationContext());
        }
        finally
        {
            if (acquired)
            {
                try
                {
                    mutex.ReleaseMutex();
                }
                catch
                {
                    // Best effort: process is exiting anyway.
                }
            }
        }
    }
}
