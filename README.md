# cycle

A .NET CLI tool that generates a Solution Filter (`.slnf`) from a list of changed files. It evaluates MSBuild project graphs — including transitive dependencies, imports, and item references — so you can scope builds and tests in CI to only what changed.

## Installation

```bash
dotnet tool install --global cycle
```

## Usage

```bash
cycle <solution-path> <output-file> [options]
```

### Arguments

| Argument | Description |
|---|---|
| `solution-path` | Path to the solution file (`.sln` or `.slnx`) |
| `output-file` | Path to write the solution filter (`.slnf`) |

### Options

| Option | Description | Default |
|---|---|---|
| `--changed-files <path>` | File containing changed file paths (one per line) | |
| `--closure` | Include transitive build dependencies (ProjectReferences) so the filter is buildable | `false` |
| `--log-level <level>` | Log verbosity: `quiet`, `minimal`, `normal`, `verbose` | `minimal` |

Changed files are read from `--changed-files` if provided, otherwise from stdin when input is piped.

## Examples

Build and test only what changed on a feature branch:

```bash
git diff --name-only origin/main...HEAD | cycle MySolution.slnx affected.slnf --closure
dotnet build affected.slnf
dotnet test affected.slnf
```

Build only what changed in the most recent commit on main:

```bash
git diff --name-only HEAD~1...HEAD | cycle MySolution.slnx affected.slnf --closure
dotnet build affected.slnf
```

Use a file list:

```bash
cycle MySolution.slnx affected.slnf --changed-files changes.txt
```

## Two-dot vs three-dot diff

The examples above use `git diff --name-only A...B` (three dots). This matters when your branch has diverged from the base:

```
         C---D---E  feature
        /
   A---B---F---G    main
```

| Syntax | What it compares | Changed files |
|---|---|---|
| `git diff --name-only main..feature` | G vs E (tip to tip) | Includes changes from F and G on main that the feature branch doesn't have |
| `git diff --name-only main...feature` | B vs E (merge base to tip) | Only changes introduced on the feature branch |

The two-dot diff (`..`) compares the two endpoints directly. If main has moved forward since the branch was created, the diff includes changes from both sides, leading to unrelated projects in the filter.

The three-dot diff (`...`) finds the common ancestor (merge base) and compares only what changed since that point. This gives you exactly the files the branch introduced, which is what you want for scoping builds.

Use three dots (`...`) for feature branches. For consecutive commits on the same branch (e.g. `HEAD~1...HEAD`), both forms are equivalent.

## Building from Source

```bash
dotnet build Cycle.slnx
dotnet test Cycle.slnx
dotnet pack Cycle.slnx
```

## License

[MIT](LICENSE)
