using Microsoft.Extensions.Logging;

namespace Cycle.Presentation.Cli.Tests;

[TestFixture]
public class CreateLoggerFactoryTests
{
    [Test]
    public void Quiet_ReturnsFactoryWithNoneLevel()
    {
        using var factory = Program.CreateLoggerFactory("quiet");
        var logger = factory.CreateLogger("Test");

        logger.IsEnabled(LogLevel.Warning).ShouldBeFalse();
        logger.IsEnabled(LogLevel.Critical).ShouldBeFalse();
    }

    [Test]
    public void Minimal_ReturnsFactoryWithWarningLevel()
    {
        using var factory = Program.CreateLoggerFactory("minimal");
        var logger = factory.CreateLogger("Test");

        logger.IsEnabled(LogLevel.Warning).ShouldBeTrue();
        logger.IsEnabled(LogLevel.Information).ShouldBeFalse();
    }

    [Test]
    public void Normal_ReturnsFactoryWithInformationLevel()
    {
        using var factory = Program.CreateLoggerFactory("normal");
        var logger = factory.CreateLogger("Test");

        logger.IsEnabled(LogLevel.Information).ShouldBeTrue();
        logger.IsEnabled(LogLevel.Debug).ShouldBeFalse();
    }

    [Test]
    public void Verbose_ReturnsFactoryWithDebugLevel()
    {
        using var factory = Program.CreateLoggerFactory("verbose");
        var logger = factory.CreateLogger("Test");

        logger.IsEnabled(LogLevel.Debug).ShouldBeTrue();
    }

    [Test]
    public void UnknownValue_DefaultsToWarning()
    {
        using var factory = Program.CreateLoggerFactory("garbage");
        var logger = factory.CreateLogger("Test");

        logger.IsEnabled(LogLevel.Warning).ShouldBeTrue();
        logger.IsEnabled(LogLevel.Information).ShouldBeFalse();
    }
}
