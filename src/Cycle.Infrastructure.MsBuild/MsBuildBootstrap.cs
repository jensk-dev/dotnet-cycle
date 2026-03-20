using Microsoft.Build.Locator;

namespace Cycle.Infrastructure.MsBuild;

public static class MsBuildBootstrap
{
    private static int _initialized;

    public static void Initialize()
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 0)
        {
            MSBuildLocator.RegisterDefaults();
        }
    }
}
