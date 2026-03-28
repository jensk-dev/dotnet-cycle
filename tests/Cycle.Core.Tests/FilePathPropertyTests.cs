using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Cycle.Core.Tests;

public sealed class FilePathPropertyTests
{
    private static Arbitrary<string> ValidFilePathStrings()
    {
        var gen = from name in Gen.Elements("file", "test", "data", "main", "app", "src", "lib")
                  from ext in Gen.Elements(".cs", ".txt", ".json", ".xml", ".csproj", ".fs")
                  select Path.Combine(Path.GetTempPath(), $"{name}{ext}");
        return gen.ToArbitrary();
    }

    [Property]
    public Property RoundTrip_FullPathIsRootedAndContainsFileName()
    {
        return Prop.ForAll(ValidFilePathStrings(), s =>
        {
            var fp = FilePath.FromString(s);
            return Path.IsPathRooted(fp.FullPath) && fp.FullPath.Contains(Path.GetFileName(s));
        });
    }

    [Property]
    public Property EqualityIsReflexive()
    {
        return Prop.ForAll(ValidFilePathStrings(), s =>
        {
            var fp = FilePath.FromString(s);
            return fp.Equals(fp);
        });
    }

    [Property]
    public Property HashCodeConsistency_EqualValuesProduceEqualHashes()
    {
        return Prop.ForAll(ValidFilePathStrings(), s =>
        {
            var fp1 = FilePath.FromString(s);
            var fp2 = FilePath.FromString(s);
            return fp1.Equals(fp2) && fp1.GetHashCode() == fp2.GetHashCode();
        });
    }

    [Property]
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

    private static Arbitrary<string> AdversarialFilePathStrings()
    {
        var gen = from count in Gen.Choose(1, 4)
                  from segments in Gen.Elements(
                          "src", "SRC", "Src", "models", "MODELS",
                          "build", "a b c", "deeply", "nested")
                      .ArrayOf(count)
                  from name in Gen.Elements("File", "FILE", "file", "my file", "data.backup")
                  from ext in Gen.Elements(".cs", ".CS", ".Cs", ".csproj", ".json")
                  let path = Path.Combine(Path.GetTempPath(), Path.Combine(segments), $"{name}{ext}")
                  where !Path.EndsInDirectorySeparator(path)
                  select path;
        return gen.ToArbitrary();
    }

    [Property]
    public Property EqualityIsSymmetric()
    {
        return Prop.ForAll(AdversarialFilePathStrings(), s =>
        {
            var a = FilePath.FromString(s);
            var b = FilePath.FromString(s);
            return a.Equals(b) == b.Equals(a);
        });
    }

    [Property]
    public Property SeparatorNormalization_ConsistentAfterFullPathResolution()
    {
        return Prop.ForAll(ValidFilePathStrings(), s =>
        {
            var fp = FilePath.FromString(s);
            var fromFullPath = FilePath.FromString(Path.GetFullPath(s));
            return fp.Equals(fromFullPath);
        });
    }
}
