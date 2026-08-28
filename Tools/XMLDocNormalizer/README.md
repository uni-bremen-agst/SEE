# XMLDocNormalizer

XMLDocNormalizer is a command-line tool for checking and normalizing C# XML documentation.
It supports syntax-based documentation checks, semantic checks for projects and solutions,
transitive exception-flow analysis, machine-readable reports, and selected automatic fixes.

## Requirements

* .NET 8 SDK

Install the SDK if necessary:

```text
Windows: winget install Microsoft.DotNet.SDK.8
macOS:   brew install dotnet@8
Linux:   sudo apt install dotnet-sdk-8.0
```

Restore and build the solution from the XMLDocNormalizer directory:

```powershell
dotnet restore .\XMLDocNormalizer.sln
dotnet build .\XMLDocNormalizer.sln
```

The examples below invoke the built Debug assembly:

```powershell
dotnet .\src\XMLDocNormalizer\bin\Debug\net8.0\XMLDocNormalizer.dll <options> <target>
```

During development, the tool can also be started through `dotnet run`:

```powershell
dotnet run --project .\src\XMLDocNormalizer\XMLDocNormalizer.csproj -- <options> <target>
```

## Basic usage

Exactly one of `--check` or `--fix` must be specified.

```text
XMLDocNormalizer (--check | --fix) [options] [target]
```

If no target is supplied, the current working directory is used.

### Check a directory

```powershell
dotnet .\src\XMLDocNormalizer\bin\Debug\net8.0\XMLDocNormalizer.dll `
    --check `
    .\src
```

### Check a project

```powershell
dotnet .\src\XMLDocNormalizer\bin\Debug\net8.0\XMLDocNormalizer.dll `
    --check `
    .\src\XMLDocNormalizer\XMLDocNormalizer.csproj
```

### Check one project from a solution

```powershell
dotnet .\src\XMLDocNormalizer\bin\Debug\net8.0\XMLDocNormalizer.dll `
    --check `
    --project XMLDocNormalizer `
    .\XMLDocNormalizer.sln
```

### Check every project in a solution

```powershell
dotnet .\src\XMLDocNormalizer\bin\Debug\net8.0\XMLDocNormalizer.dll `
    --check `
    --full `
    .\XMLDocNormalizer.sln
```

## Input behavior

XMLDocNormalizer accepts:

* a single C# source file;
* a directory containing C# source files;
* a `.csproj` project;
* a `.sln` solution.

File and directory inputs are processed without an MSBuild semantic project model.
Project and solution inputs are loaded through MSBuild/Roslyn and enable semantic analysis,
including exception-flow analysis.

For a solution input:

* `--full` analyzes every project in the solution;
* `--project <name>` analyzes the specified project;
* without either option, XMLDocNormalizer selects the project whose name matches the solution file name.

`--full` and `--project` cannot be used together.

Generated files and test files are excluded by default. Use `--include-generated` and
`--include-tests` to include them.

## Check and fix modes

### `--check`

Runs the configured checks without modifying source files.

```powershell
dotnet .\src\XMLDocNormalizer\bin\Debug\net8.0\XMLDocNormalizer.dll `
    --check `
    .\src
```

### `--fix`

Runs the available source rewriters on file or directory inputs.

```powershell
dotnet .\src\XMLDocNormalizer\bin\Debug\net8.0\XMLDocNormalizer.dll `
    --fix `
    .\src
```

Project and solution inputs are primarily intended for semantic checking. The current
project/solution pipeline performs analysis and reporting rather than source rewriting.

### `--test`

When used with file/directory fix mode, the original source file is left untouched and a
timestamped `.bak` copy is created and rewritten instead.

```powershell
dotnet .\src\XMLDocNormalizer\bin\Debug\net8.0\XMLDocNormalizer.dll `
    --fix `
    --test `
    .\src
```

### `--clean-backups`

Deletes timestamped backup files previously created by XMLDocNormalizer below the target
location before processing starts.

## Output formats

Use `--format` to select the findings output format:

* `console` - human-readable console output; default;
* `json` - JSON findings report;
* `sarif` - SARIF findings report.

### JSON

```powershell
dotnet .\src\XMLDocNormalizer\bin\Debug\net8.0\XMLDocNormalizer.dll `
    --check `
    --project XMLDocNormalizer `
    --format json `
    --output .\artifacts\findings.json `
    .\XMLDocNormalizer.sln
```

Without `--output`, JSON output defaults to:

```text
artifacts/findings.json
```

### SARIF

```powershell
dotnet .\src\XMLDocNormalizer\bin\Debug\net8.0\XMLDocNormalizer.dll `
    --check `
    --project XMLDocNormalizer `
    --format sarif `
    --output .\artifacts\findings.sarif `
    .\XMLDocNormalizer.sln
```

Without `--output`, SARIF output defaults to:

```text
artifacts/findings.sarif
```

## Exception analysis

Exception analysis is configured with:

```text
--exception-analysis-mode <mode>
```

The default is `solution-transitive`.

### `direct`

Reports exception documentation based on directly thrown exceptions only.

Alias:

```text
d
```

### `project-transitive-declared-exceptions`

Reports direct exceptions and follows calls within the reporting scope, while restricting
transitive exception types to exception types declared in that scope.

Aliases:

```text
ptd
declared
project-declared
project-transitive-declared
```

### `project-transitive`

Follows exception flow transitively within the reporting scope.

Aliases:

```text
pt
project
```

### `solution-transitive`

Follows exception flow across the loaded solution project-reference closure.

Aliases:

```text
st
solution
```

Example:

```powershell
dotnet .\src\XMLDocNormalizer\bin\Debug\net8.0\XMLDocNormalizer.dll `
    --check `
    --project XMLDocNormalizer `
    --exception-analysis-mode solution-transitive `
    .\XMLDocNormalizer.sln
```

## Comparing exception-analysis modes

Use `--compare-exception-analysis-modes` to execute all four exception-analysis modes in
isolated child processes.

This option requires `--check` and a project or solution input.

```powershell
dotnet .\src\XMLDocNormalizer\bin\Debug\net8.0\XMLDocNormalizer.dll `
    --check `
    --project XMLDocNormalizer `
    --compare-exception-analysis-modes `
    --format json `
    --output .\artifacts\exception-comparison.json `
    .\XMLDocNormalizer.sln
```

With the output path above, XMLDocNormalizer writes per-mode reports such as:

```text
artifacts/exception-comparison_direct.json
artifacts/exception-comparison_project-transitive-declared-exceptions.json
artifacts/exception-comparison_project-transitive.json
artifacts/exception-comparison_solution-transitive.json
```

and the aggregate comparison report:

```text
artifacts/exception-comparison_exception-analysis-mode-comparison.json
```

### Repeated measurements

`--exception-analysis-comparison-runs <n>` controls the number of measured isolated runs
per mode. The default is `1`.

```powershell
--exception-analysis-comparison-runs 5
```

For more than one measured run, the comparison rotates mode order and reports statistics
including median, mean, minimum, maximum, and standard deviation.

`--exception-analysis-comparison-warmup-runs <n>` controls the number of warmup runs per
mode. Warmups are excluded from timing statistics. The default is `0`.

Example:

```powershell
dotnet .\src\XMLDocNormalizer\bin\Debug\net8.0\XMLDocNormalizer.dll `
    --check `
    --project XMLDocNormalizer `
    --compare-exception-analysis-modes `
    --exception-analysis-comparison-warmup-runs 1 `
    --exception-analysis-comparison-runs 5 `
    --output .\artifacts\exception-comparison.json `
    .\XMLDocNormalizer.sln
```

## Value-documentation modes

Missing `<value>` documentation is controlled with:

```text
--value-documentation-mode <mode>
```

The default is `all-readable-properties`.

Supported modes are:

* `disabled` - disables missing-value documentation findings;
* `all-readable-properties` - checks all readable properties;
* `exclude-dto-like-types` - excludes DTO-like types from the general property requirement;
* `indexers-only` - limits the missing-value requirement to indexers.

Aliases:

```text
disabled:                off, none
all-readable-properties: all, readable-properties, strict
exclude-dto-like-types:  exclude-dto-like, exclude-dto, non-dto, non-dto-like
indexers-only:            indexer-only, indexers
```

Example:

```powershell
dotnet .\src\XMLDocNormalizer\bin\Debug\net8.0\XMLDocNormalizer.dll `
    --check `
    --value-documentation-mode exclude-dto-like-types `
    .\XMLDocNormalizer.sln
```

## Comparing value-documentation modes

`--compare-value-documentation-modes` runs all value-documentation modes and writes a JSON
comparison report plus one findings report per mode.

This option requires `--check` and cannot be combined with
`--compare-exception-analysis-modes`.

```powershell
dotnet .\src\XMLDocNormalizer\bin\Debug\net8.0\XMLDocNormalizer.dll `
    --check `
    --compare-value-documentation-modes `
    --output .\artifacts\value-comparison.json `
    .\XMLDocNormalizer.sln
```

## Documentation policy options

The following switches adjust selected documentation requirements:

| Option                       | Effect                                                        |
| ---------------------------- | ------------------------------------------------------------- |
| `--check-enum-members`       | Requires enum-member XML documentation. This is the default.  |
| `--no-check-enum-members`    | Disables the enum-member documentation requirement.           |
| `--require-field-summary`    | Requires non-empty summaries for fields. This is the default. |
| `--no-require-field-summary` | Disables the field-summary requirement.                       |

## File-selection options

| Option                | Effect                                              |
| --------------------- | --------------------------------------------------- |
| `--include-generated` | Includes generated files in analysis and metrics.   |
| `--include-tests`     | Includes test source files in analysis and metrics. |

Without these options, generated and test files are excluded.

## Statistics output

`--enable-statistics` enables the additional study/statistics output. It requires `--check`.
The statistics are written as JSON and as a text report.

```powershell
dotnet .\src\XMLDocNormalizer\bin\Debug\net8.0\XMLDocNormalizer.dll `
    --check `
    --project XMLDocNormalizer `
    --enable-statistics `
    --statistics-output .\artifacts\statistics.json `
    .\XMLDocNormalizer.sln
```

Output-path resolution:

1. `--statistics-output <path>` when explicitly provided;
2. `<output>_statistics.json` when `--output` is set;
3. `artifacts/statistics.json` otherwise.

A corresponding `.txt` report is written next to the statistics JSON file.

## Verbose logging

Enable additional progress and diagnostic output with:

```text
--verbose
```

or:

```text
-v
```

## CLI option reference

| Option                                            | Description                                                                          |
| ------------------------------------------------- | ------------------------------------------------------------------------------------ |
| `--check`                                         | Analyze without modifying source files.                                              |
| `--fix`                                           | Apply supported source rewrites.                                                     |
| `--full`                                          | Analyze all projects in a solution.                                                  |
| `--project <name>`                                | Analyze a specific project in a solution.                                            |
| `--test`                                          | In file/directory fix mode, rewrite timestamped backup copies instead of originals.  |
| `--clean-backups`                                 | Delete XMLDocNormalizer timestamped backup files below the target before processing. |
| `--verbose`, `-v`                                 | Enable verbose logging.                                                              |
| `--format <console\|json\|sarif>`                 | Select findings output format.                                                       |
| `--output <path>`                                 | Set the findings/comparison output path.                                             |
| `--exception-analysis-mode <mode>`                | Select exception-analysis mode.                                                      |
| `--compare-exception-analysis-modes`              | Compare all four exception-analysis modes.                                           |
| `--exception-analysis-comparison-runs <n>`        | Set measured runs per exception mode; must be greater than zero.                     |
| `--exception-analysis-comparison-warmup-runs <n>` | Set warmup runs per exception mode; must be zero or greater.                         |
| `--value-documentation-mode <mode>`               | Select missing-`<value>` analysis mode.                                              |
| `--compare-value-documentation-modes`             | Compare all value-documentation modes.                                               |
| `--check-enum-members`                            | Enable enum-member documentation checks.                                             |
| `--no-check-enum-members`                         | Disable enum-member documentation checks.                                            |
| `--require-field-summary`                         | Enable field-summary requirements.                                                   |
| `--no-require-field-summary`                      | Disable field-summary requirements.                                                  |
| `--enable-statistics`                             | Generate additional statistics reports.                                              |
| `--statistics-output <path>`                      | Set the statistics JSON output path.                                                 |
| `--include-generated`                             | Include generated files.                                                             |
| `--include-tests`                                 | Include test files.                                                                  |
| `--help`, `-h`                                    | Print CLI help.                                                                      |

## Exit codes

For normal check/fix runs:

| Code | Meaning                                                          |
| ---: | ---------------------------------------------------------------- |
|  `0` | Execution completed without findings.                            |
|  `1` | Execution completed and findings were reported.                  |
|  `2` | Invalid command-line arguments or configuration.                 |
|  `3` | No default project matching the solution name could be selected. |

The dedicated exception- and value-comparison commands return `0` when the comparison
runner completes successfully, even when the generated mode reports contain findings.

## Tests and self-analysis

The normal test suite and the explicit solution-transitive self-analysis are documented in:

```text
Tests/XMLDocNormalizerTests/README.md
```

Run the regular test suite with:

```powershell
dotnet test .\XMLDocNormalizer.sln --no-build
```

The expensive solution-transitive self-analysis is intentionally excluded from the default
test run and is started explicitly as documented in the test README.
