using Microsoft.Build.Locator;

namespace Cycle.Infrastructure;

public static class MsBuildBootstrap
{
    private static int _initialized;

    public static void Initialize()
    {
        if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 0)
        {
            try
            {
                MSBuildLocator.RegisterDefaults();
            }
            catch
            {
                Interlocked.Exchange(ref _initialized, 0);
                throw;
            }
        }
    }
}
