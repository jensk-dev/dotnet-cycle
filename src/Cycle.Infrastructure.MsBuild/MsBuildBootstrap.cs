using Microsoft.Build.Locator;

namespace Cycle.Infrastructure.MsBuild;

public static class MsBuildBootstrap
{
    public static void Initialize() => MSBuildLocator.RegisterDefaults();
}
