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

## Terminal progress

Use `--progress auto|on|off` independently of `--verbose`:

```powershell
dotnet run --project src/XMLDocNormalizer -- --check --progress on --exception-analysis-mode solution-transitive MySolution.sln
```

`auto` (the default) enables progress only when stdin, stdout and stderr are interactive,
the terminal is suitable, and no CI environment is detected. `on` enables it explicitly;
redirected stderr receives only phase starts and final summaries. `off` creates no display,
timer or progress observer. Progress always uses stderr; existing console reports and
JSON/SARIF report files retain their output contracts. No ANSI sequences or colors are required.

On a verified Windows console, the compact display groups work into Completed, Current work,
and Pending. Exactly one blank line separates adjacent visible areas. Uppercase headings use
green, the existing orange/gold activity color, and muted gray respectively; analysis details
retain their uniform two-space indentation and neutral color. The gold Current work heading
is separate from the single highlighted activity. Empty areas disappear. Completed entries turn green at their proved scope boundary,
move exactly once, and retain final times and counters. Current work has at most one orange/gold
activity and neutral file/root/context counters. Pending entries are gray and have no invented
elapsed times or percentages. Documentation and exception flow remain interleaved; both share
Analyze while unfinished, and a completed part disappears from Current work immediately.

The Current line uses available detector or root/member data with an honest general fallback.
`--verbose` adds available admission counters only inside the unfinished analysis. No transient
member, detector or activity snapshots remain after completion. Findings, warnings, hints, coverage,
SLOC and statistics belong exclusively to the existing reporters; the dashboard does not repeat
those results. Its successful final view contains completed work, frozen times/counters and total
time in the header. Actual failures are red, interrupted work is identified without false green.

Only changed rows are updated. Normal refreshes neither change states/counters nor move entries.
Real scope transitions may change area membership and height; the existing incremental region
clears removed rows and shortened tails. Actual console messages appear above a reanchored
region, while empty/file-only reporting does not disturb it. Native Windows stderr cell APIs are
used without unverified ANSI assumptions or an external renderer library. Status markers also
work without color. The native orange/gold fallback is DarkYellow (palette index 6), whose shade
depends on the user's palette. Background/global attributes and `NO_COLOR` remain respected.
Redirects, CI and unsuitable terminals receive bounded categorized text transitions on stderr.

Below 80 columns the initial layout uses shorter labels. Below 40 columns, with insufficient
height, when a wide layout no longer fits or after buffer reflow, quiet text transitions take over.
Non-ASCII characters in the live region appear as `?` for predictable cell widths; ordinary
messages and report files retain Unicode. The display never treats findings as an execution error.

Compilation preparation ends at the existing eager project-context/session preparation boundary.
Missing compilations leave preparation interrupted. Later cached accesses and demand-driven
supporting work stay inside Analyze without reopening completed preparation. Intermediate
aggregation and per-file reporting also remain in Analyze. Final reporting starts only at the
explicit end of all required analysis scopes. Paths without a separable final report omit that step.
The real finalization transition is painted immediately. If its visible interval is shorter than
1,000 ms, a working interactive display retains that frame briefly before the green end view.
This presentation pause happens after reporting work is complete and its elapsed time is frozen;
a subsecond finalization still correctly shows `00:00`. Time already visible is credited, actual
finalization of at least one second adds no wait, and the presentation pause is excluded from
the displayed total time. Redirects, CI, disabled progress and text fallback add no pause.
Ordinary counter/time refreshes retain their 500 ms throttle.
The in-process mode comparison keeps its actual later preparation pending/ongoing until its real
boundary. `Finish()` interrupts unfinished work; it never invents a successful scope completion.
The documentation bar explicitly labels eligible files and counts them after their regular detectors finish, not detector
invocations. Unavailable compilations or syntax trees leave those files unfinished.
Verbose loading messages can mention all discovered documents; that candidate count is not
the filtered eligible-file denominator used by the bar.

Exception analysis in all four modes reports roots started/completed. Transitive sessions also
report dynamically discovered context nodes and processed context workitems. These are activity
counts, not a fixed final total or a completion certificate. There is no context percentage or ETA.
An active root still shows elapsed time and time since the last observed activity when its counts
do not change. The display refresh is limited to twice per second; refreshes never advance counters.
Direct mode omits context/workitem rows entirely.
Explicit `Incomplete` coverage remains separate
from technical failure in the final status.

`--verbose` adds stable rows for the active detector/member and available admission
work counters. It uses existing work notifications, not an additional analysis. Historical/external
worker calls and isolated mode-comparison children expose a coarse process phase only; their wire
protocol carries no new progress telemetry. SCC counts and a stable global root total are not
computed just for display. Ctrl+C leaves an interrupted final display without changing cancellation policy;
forced process termination cannot guarantee cleanup.

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

## Analysis coverage and completion

The finding count and analysis coverage are separate results. **Zero findings does
not by itself mean that analysis was complete.** Console summaries now show both;
JSON includes `Analysis`, and SARIF includes `runs[].properties.analysis`.

`Complete` means that the configured checks completed for the selected scope and
exception mode. `Incomplete` means that some required coverage is missing.
`Unsupported` means that an input or semantic profile cannot be fully analyzed.
These are distinct from technical execution failure. File/directory inputs run
syntax checks without the semantic exception analysis available for projects and
solutions, so their overall semantic coverage is `Unsupported`.

Exit codes are unchanged: a normal run can return 0 with incomplete coverage.
CI should inspect coverage as well as findings. See the
[coverage contract, examples, mode boundaries and failure behavior](docs/analysis-coverage.md).

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

## Opt-in real-world evaluation

The pinned, opt-in E1 harness and its explicit package preparation and Source Link run
commands are maintained in the separate
[XMLDocNormalizer-Evaluation repository](https://github.com/Cyclone1337/XMLDocNormalizer-Evaluation).
The harness and its evaluation-specific tests live there; normal product tests remain in SEE.

Generated inputs and reports remain below the ignored `artifacts/real-world-evaluation`
workspace and are not part of the normal CLI or test execution.

G1 additionally permits explicitly enabled, bounded candidate lookup in loaded
metadata references, local .NET packs/shared frameworks, and the resolved NuGet
global-packages folder. These sources never establish identity: unchanged P5
validation must accept every exact reference binary, or reconstruction fails closed.

G2 adds an independent, explicitly enabled Portable PDB candidate pipeline for
embedded symbols, known local paths, safe sibling/root probes, and configured
`.nupkg`/`.snupkg` archives. Every candidate must pass the unchanged P4B exact
identity and checksum validation. Explicit PDB paths remain authoritative, and
the local G2 pipeline performs no network access, restore, build, global scan,
or persistent caching. See the external [G2 evaluation report](https://github.com/Cyclone1337/XMLDocNormalizer-Evaluation/blob/main/historical/G2-portable-pdb-acquisition.md) for the
artifact analysis and security boundary.
