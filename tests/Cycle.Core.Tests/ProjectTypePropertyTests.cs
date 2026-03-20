using FsCheck;
using FsCheck.Fluent;

namespace Cycle.Core.Tests;

[TestFixture]
public class ProjectTypePropertyTests
{
    private static Arbitrary<ProjectType> AllProjectTypes()
    {
        var gen = Gen.Elements(Enum.GetValues<ProjectType>());
        return gen.ToArbitrary();
    }

    [FsCheck.NUnit.Property]
    public Property ExtensionRoundTrip()
    {
        return Prop.ForAll(AllProjectTypes(), type =>
        {
            var ext = type.ToFileExtension();
            var success = ProjectTypeExtensions.TryFromExtension(ext, out var roundTripped);
            return success && roundTripped == type;
        });
    }

    [FsCheck.NUnit.Property]
    public Property GlobContainsExtension()
    {
        return Prop.ForAll(AllProjectTypes(), type =>
            type.ToFileGlob().EndsWith(type.ToFileExtension(), StringComparison.InvariantCulture));
    }

    [FsCheck.NUnit.Property]
    public Property AllEnumValues_DoNotThrow()
    {
        return Prop.ForAll(AllProjectTypes(), type =>
        {
            _ = type.ToFileExtension();
            _ = type.ToFileGlob();
            return true;
        });
    }
}
