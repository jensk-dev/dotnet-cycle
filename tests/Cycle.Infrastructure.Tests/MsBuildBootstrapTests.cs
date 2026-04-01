namespace Cycle.Infrastructure.Tests;

public sealed class MsBuildBootstrapTests
{
    [Fact]
    public void Initialize_CalledTwice_DoesNotThrow()
    {
        MsBuildBootstrap.Initialize();
        Should.NotThrow(MsBuildBootstrap.Initialize);
    }
}
