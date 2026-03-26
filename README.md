# cycle

A .NET CLI tool that determines which projects in a solution are affected by a set of changed files. It evaluates MSBuild project graphs — including transitive dependencies, imports, and item references — so you can scope builds and tests to only what changed.

## Installation

```bash
dotnet tool install --global cycle
```

## Usage

```bash
cycle <solution-path> [options]
```

### Arguments

| Argument | Description |
|---|---|
| `solution-path` | Path to the solution file (`.sln` or `.slnx`) |

### Options

| Option | Description | Default |
|---|---|---|
| `--changed-files <path>` | File containing changed file paths (one per line) | |
| `--stdin` | Read changed file paths from stdin | |
| `--output <format>` | Output format: `json`, `build`, or `paths` | `json` |
| `--output-file <path>` | Write output to a file instead of stdout | |
| `--include <types>` | Filter by project type (`csproj`, `fsproj`, `vbproj`, `sqlproj`, `dtproj`, `proj`) | |
| `--include-property <names>` | MSBuild properties to include in output | |
| `--log-level <level>` | Log verbosity: `quiet`, `minimal`, `normal`, `verbose` | `minimal` |

### Output Formats

- **json** — JSON array of affected projects with name, path, type, and optional properties
- **build** — MSBuild Traversal SDK project file (`Microsoft.Build.Traversal`) with `ProjectReference` items
- **paths** — Plain text, one absolute project path per line

## Examples

Pipe changed files from git:

```bash
git diff --name-only origin/main | cycle MySolution.slnx --stdin
```

Use a file list:

```bash
cycle MySolution.slnx --changed-files changes.txt --output paths
```

Generate a traversal project for selective builds:

```bash
git diff --name-only origin/main | cycle MySolution.slnx --stdin --output build --output-file affected.proj
dotnet build affected.proj
```

Filter to only C# projects and include a custom property:

```bash
git diff --name-only origin/main | cycle MySolution.slnx --stdin --include csproj --include-property IsTestProject
```

## Building from Source

```bash
dotnet build Cycle.slnx
dotnet test Cycle.slnx
dotnet pack Cycle.slnx
```

## License

[MIT](LICENSE)
