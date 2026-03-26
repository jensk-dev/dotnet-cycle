using FsCheck;
using FsCheck.Fluent;

namespace Cycle.Core.Tests;

[TestFixture]
public class FilePathPropertyTests
{
    private static Arbitrary<string> ValidFilePathStrings()
    {
        var gen = from name in Gen.Elements("file", "test", "data", "main", "app", "src", "lib")
                  from ext in Gen.Elements(".cs", ".txt", ".json", ".xml", ".csproj", ".fs")
                  select Path.Combine(Path.GetTempPath(), $"{name}{ext}");
        return gen.ToArbitrary();
    }

    [FsCheck.NUnit.Property]
    public Property RoundTrip_FullPathIsRootedAndContainsFileName()
    {
        return Prop.ForAll(ValidFilePathStrings(), s =>
        {
            var fp = FilePath.FromString(s);
            return Path.IsPathRooted(fp.FullPath) && fp.FullPath.Contains(Path.GetFileName(s));
        });
    }

    [FsCheck.NUnit.Property]
    public Property EqualityIsReflexive()
    {
        return Prop.ForAll(ValidFilePathStrings(), s =>
        {
            var fp = FilePath.FromString(s);
            return fp.Equals(fp);
        });
    }

    [FsCheck.NUnit.Property]
    public Property HashCodeConsistency_EqualValuesProduceEqualHashes()
    {
        return Prop.ForAll(ValidFilePathStrings(), s =>
        {
            var fp1 = FilePath.FromString(s);
            var fp2 = FilePath.FromString(s);
            return fp1.Equals(fp2) && fp1.GetHashCode() == fp2.GetHashCode();
        });
    }

    [FsCheck.NUnit.Property]
    public Property TryFromString_ConsistentWithFromString()
    {
        return Prop.ForAll(ValidFilePathStrings(), s =>
        {
            var tryResult = FilePath.TryFromString(s, out var tryFp);
            try
            {
                var fp = FilePath.FromString(s);
                return tryResult && tryFp!.Value.Equals(fp);
            }
            catch (ArgumentException)
            {
                return !tryResult;
            }
        });
    }
}
