using Microsoft.Build.Locator;

namespace Cycle.Infrastructure;

public static class MsBuildBootstrap
{
    private static readonly object InitLock = new();
    private static bool _initialized;

    public static void Initialize()
    {
        // Callers must block until registration has completed, not just until it has
        // started. A caller that returns early can trigger JIT compilation of code
        // referencing Microsoft.Build types before the locator's assembly resolver
        // is registered, which fails with FileNotFoundException.
        lock (InitLock)
        {
            if (_initialized)
            {
                return;
            }

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

            _initialized = true;
        }
    }
}
