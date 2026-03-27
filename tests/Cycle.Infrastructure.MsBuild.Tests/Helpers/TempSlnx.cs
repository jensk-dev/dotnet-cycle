namespace Cycle.Infrastructure.MsBuild.Tests.Helpers;

public static class TempSlnx
{
    public static string Create(string directory, params TempCsProj[] projects) =>
        Create(directory, projects.Select(p => p.ProjectFilePath).ToArray());

    public static string Create(string directory, params string[] projectPaths)
    {
        var slnPath = Path.Combine(directory, "Test.slnx");
        var projectEntries = string.Join(Environment.NewLine,
            projectPaths.Select(p =>
                $"    <Project Path=\"{Path.GetRelativePath(directory, p)}\" />"));

        var content = $"""
                       <Solution>
                         <Folder Name="/src/">
                       {projectEntries}
                         </Folder>
                       </Solution>
                       """;

        File.WriteAllText(slnPath, content);
        return slnPath;
    }
}
