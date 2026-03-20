namespace Cycle.Infrastructure.MsBuild.Tests;

[TestFixture]
public class MsBuildBootstrapTests
{
    [Test]
    public void Initialize_CalledTwice_DoesNotThrow()
    {
        MsBuildBootstrap.Initialize();
        Should.NotThrow(MsBuildBootstrap.Initialize);
    }
}
