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
                var runtimeMajor = Environment.Version.Major;

                var match = MSBuildLocator.QueryVisualStudioInstances()
                    .Where(i => i.Version.Major == runtimeMajor)
                    .OrderByDescending(i => i.Version)
                    .FirstOrDefault();

                if (match is not null)
                {
                    MSBuildLocator.RegisterInstance(match);
                }
                else
                {
                    MSBuildLocator.RegisterDefaults();
                }
            }
            catch
            {
                Interlocked.Exchange(ref _initialized, 0);
                throw;
            }
        }
    }
}
