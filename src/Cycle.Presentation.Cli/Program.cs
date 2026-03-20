using Cycle.Infrastructure.MsBuild;

namespace Cycle.Presentation.Cli;

public static class Program
{
    public static async Task Main(string[] args)
    {
        MsBuildBootstrap.Initialize();
        
        var solutionPath = args[0];
        var reader = new MsBuildSolutionReader();

        var projects = await reader.GetProjectsAsync(solutionPath, CancellationToken.None);

        foreach (var project in projects)
        {
            Console.WriteLine("{0} ({1})", project.Name, project.FilePath);
        }
    }
}
