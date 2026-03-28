using Cycle.Infrastructure.MsBuild;

namespace Cycle.Tests.Common;

public class MsBuildFixture
{
    public MsBuildFixture() => MsBuildBootstrap.Initialize();
}
