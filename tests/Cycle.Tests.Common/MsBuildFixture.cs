using Cycle.Infrastructure;

namespace Cycle.Tests.Common;

public class MsBuildFixture
{
    public MsBuildFixture() => MsBuildBootstrap.Initialize();
}
