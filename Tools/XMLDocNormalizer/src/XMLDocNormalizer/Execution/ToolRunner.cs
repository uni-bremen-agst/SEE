using System.Diagnostics;
using System.Text;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure;
using XMLDocNormalizer.Checks.Infrastructure.Namespace;
using XMLDocNormalizer.Cli;
using XMLDocNormalizer.Cli.Output;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.IO;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.Keys;
using XMLDocNormalizer.Reporting;
using XMLDocNormalizer.Reporting.Abstractions;
using XMLDocNormalizer.Reporting.Console;
using XMLDocNormalizer.Rewriting;
using XMLDocNormalizer.Utils;

namespace XMLDocNormalizer.Execution
{
    /// <summary>
    /// Executes the tool logic (check/fix modes) based on parsed options.
    /// </summary>
    /// <remarks>
    /// This class separates the orchestration logic from the CLI entry point.
    /// It contains the end-to-end pipeline for processing files:
    /// discovery, parsing, validation, rewriting, and reporting.
    /// Supports both individual files/folders and project/solution analysis.
    /// </remarks>
    internal sealed class ToolRunner
    {
        /// <summary>
        /// Runs the tool according to the specified options.
        /// </summary>
        /// <param name="options">The parsed command-line options.</param>
        /// <returns>The aggregated run result.</returns>
        public static RunResult Run(ToolOptions options)
        {
            ConsoleLogger.VerboseEnabled = options.Verbose;
            ConsoleLogger.Info($"Running XMLDocNormalizer with target: {options.TargetPath}");

            if (!File.Exists(options.TargetPath) && !Directory.Exists(options.TargetPath))
            {
                throw new ArgumentException($"Invalid target path: {options.TargetPath}", nameof(options));
            }

            if (options.TargetPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                options.TargetPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                ConsoleLogger.Info("Project/solution detected. Running with semantic analysis.");
                return RunProjectOrSolution(options.TargetPath, options);
            }

            ConsoleLogger.InfoVerbose("File or directory detected. Running without semantic analysis.");
            List<string> files = FileDiscovery.EnumerateCsFiles(options.TargetPath);

            if (options.CheckOnly)
            {
                ConsoleLogger.InfoVerbose("Check-only mode enabled. No changes will be made.");
                return RunCheck(files, options);
            }

            ConsoleLogger.InfoVerbose("Fix mode enabled.");
            return RunFix(files, options);
        }

        /// <summary>
        /// Runs the tool on a C# project or solution using semantic analysis.
        /// Supports .sln files, single projects, and the --full / --project flags.
        /// </summary>
        /// <param name="path">Path to the .csproj or .sln file.</param>
        /// <param name="options">Tool options controlling check/fix, verbosity, full analysis, etc.</param>
        /// <returns>The aggregated run result containing counts and findings.</returns>
        private static RunResult RunProjectOrSolution(string path, ToolOptions options)
        {
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }

            ConsoleLogger.InfoVerbose($"Opening: {path}");
            Stopwatch stopwatch = Stopwatch.StartNew();

            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            IProgress<ProjectLoadProgress> progress = new Progress<ProjectLoadProgress>(p =>
            {
                ConsoleLogger.InfoProgress($"[MSBuild] {p.Operation} - {p.FilePath}");
            });

            RunResult result = new();
            IFindingsReporter reporter = FindingsReporterFactory.Create(options);
            List<Project> projectsToAnalyze = LoadProjectsToAnalyze(path, options, workspace, progress);

            ProjectClosureSemanticContext semanticContext =
                ProjectClosureSemanticContextBuilder.Build(
                    projectsToAnalyze,
                    options.XmlDocOptions.ExceptionAnalysisMode);

            // Count total documents
            int totalDocuments = projectsToAnalyze.Sum(p => p.Documents.Count());
            ConsoleLogger.InfoVerbose($"Processing {totalDocuments} document(s)...");

            int currentDocument = 0;
            foreach (Project project in projectsToAnalyze)
            {
                NamespaceDocumentationAggregator namespaceAggregator =
                    new(options.XmlDocOptions.RequireDocumentationForNamespaces);

                if (projectsToAnalyze.Count > 1)
                {
                    ConsoleLogger.Info($"Analyzing project: {project.Name}");
                }
                Compilation? compilation = project.GetCompilationAsync().GetAwaiter().GetResult();

                if (compilation == null)
                {
                    ConsoleLogger.Warn($"Compilation for project {project.Name} is null. Skipping this project.");
                    continue;
                }

                foreach (Document document in project.Documents)
                {
                    currentDocument++;
                    ConsoleLogger.InfoVerbose($"[{currentDocument}/{totalDocuments}] {document.Name}");

                    string? filePath = document.FilePath;
                    if (filePath == null)
                    {
                        continue;
                    }

                    if (ToolFileFilter.ShouldExclude(filePath, options))
                    {
                        continue;
                    }

                    SyntaxTree? tree = document.GetSyntaxTreeAsync().GetAwaiter().GetResult();
                    if (tree == null)
                    {
                        continue;
                    }

                    AccumulateSloc(result, tree, filePath, options);
                    IReadOnlyDictionary<string, int> fileTotals = DocumentationStatisticsCollector.Collect(tree);
                    result.AccumulateTotals(fileTotals);

                    SemanticModel semanticModel = compilation.GetSemanticModel(tree);

                    List<Finding> findings = new(XmlDocBasicDetector.FindBasicSmells(tree, filePath, options.XmlDocOptions, namespaceAggregator));

                    // Common syntax detectors.
                    foreach (XmlDocDetectorCatalog.SyntaxDetector detector in XmlDocDetectorCatalog.SyntaxDetectors)
                    {
                        findings.AddRange(detector(tree, filePath));
                    }

                    // Common semantic detectors.
                    foreach (XmlDocDetectorCatalog.SemanticDetector detector in XmlDocDetectorCatalog.SemanticDetectors)
                    {
                        findings.AddRange(detector(tree, filePath, semanticModel));
                    }

                    // Exception semantic detector requires project-closure context.
                    findings.AddRange(
                        XmlDocExceptionSemanticDetector.FindExceptionSmells(
                            tree,
                            filePath,
                            semanticModel,
                            semanticContext,
                            options.XmlDocOptions));

                    result.AccumulateFindings(findings);
                    reporter.ReportFile(filePath, findings);
                }

                FlushNamespaceFindings(namespaceAggregator, result, reporter);
            }

            stopwatch.Stop();
            result.AnalysisDurationMs = stopwatch.ElapsedMilliseconds;
            CompleteReporting(reporter, result);
            ConsoleLogger.Info($"\nAnalysis finished in {stopwatch.ElapsedMilliseconds} ms.");
            return result;
        }

        /// <summary>
        /// Loads the projects that should be analyzed from a project or solution path.
        /// </summary>
        /// <param name="path">Path to the .csproj or .sln file.</param>
        /// <param name="options">Tool options controlling project selection.</param>
        /// <param name="workspace">The MSBuild workspace.</param>
        /// <param name="progress">The project load progress callback.</param>
        /// <returns>The selected projects.</returns>
        private static List<Project> LoadProjectsToAnalyze(
            string path,
            ToolOptions options,
            MSBuildWorkspace workspace,
            IProgress<ProjectLoadProgress> progress)
        {
            List<Project> projectsToAnalyze = new();

            if (path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                Project project = workspace.OpenProjectAsync(path, progress)
                                           .GetAwaiter()
                                           .GetResult();
                projectsToAnalyze.Add(project);
                ConsoleLogger.Info($"Analyzing single project: {project.Name}");
                return projectsToAnalyze;
            }

            if (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                Solution solution = workspace.OpenSolutionAsync(path, progress)
                                             .GetAwaiter()
                                             .GetResult();
                ConsoleLogger.InfoVerbose($"Loaded {solution.Projects.Count()} project(s) in solution.");

                if (options.FullAnalysis)
                {
                    projectsToAnalyze.AddRange(solution.Projects);
                    ConsoleLogger.InfoVerbose("Full analysis enabled: all projects in solution will be analyzed.");
                    return projectsToAnalyze;
                }

                if (!string.IsNullOrWhiteSpace(options.ProjectName))
                {
                    Project? project = solution.Projects.FirstOrDefault(p =>
                        string.Equals(p.Name, options.ProjectName, StringComparison.OrdinalIgnoreCase));

                    if (project == null)
                    {
                        ConsoleLogger.Warn($"Project '{options.ProjectName}' was not found in solution '{solution.FilePath}'.");
                        Environment.Exit(ToolExitCodes.InvalidArguments);
                    }

                    projectsToAnalyze.Add(project);
                    ConsoleLogger.Info($"Analyzing project: {project.Name}");
                    return projectsToAnalyze;
                }

                string solutionName = Path.GetFileNameWithoutExtension(path);
                Project? defaultProject = solution.Projects.FirstOrDefault(p =>
                    string.Equals(p.Name, solutionName, StringComparison.OrdinalIgnoreCase));

                if (defaultProject != null)
                {
                    projectsToAnalyze.Add(defaultProject);
                    ConsoleLogger.Info($"Analyzing project: {defaultProject.Name}");
                    return projectsToAnalyze;
                }

                ConsoleLogger.Warn($"No project matching solution name '{solutionName}' found.");
                Environment.Exit(ToolExitCodes.ProjectNotFound);
            }

            throw new ArgumentException($"Invalid target path: {path}. Must be .csproj or .sln.", nameof(path));
        }

        /// <summary>
        /// Executes the comparison mode for all supported exception analysis strategies.
        /// </summary>
        /// <param name="options">
        /// The parsed tool options. The target must point to a project or solution because
        /// comparison mode relies on semantic analysis and project-closure exception analysis.
        /// </param>
        /// <returns>
        /// The internal comparison execution result containing the shared baseline, all mode-specific
        /// runs, and their timing information.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the target path is invalid or does not point to a supported project/solution input.
        /// </exception>
        internal static ExceptionComparisonExecutionResult RunComparison(ToolOptions options)
        {
            ConsoleLogger.VerboseEnabled = options.Verbose;
            ConsoleLogger.Info($"Running XMLDocNormalizer comparison with target: {options.TargetPath}");

            if (!File.Exists(options.TargetPath) && !Directory.Exists(options.TargetPath))
            {
                throw new ArgumentException($"Invalid target path: {options.TargetPath}", nameof(options));
            }

            if (!options.TargetPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) &&
                !options.TargetPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "The comparison mode currently supports only project and solution inputs.",
                    nameof(options));
            }

            return RunProjectOrSolutionComparison(options.TargetPath, options);
        }

        /// <summary>
        /// Executes all exception analysis modes for a project or solution while reusing a shared baseline.
        /// </summary>
        /// <param name="path">The path to the project or solution file.</param>
        /// <param name="options">The base tool options.</param>
        /// <returns>
        /// The complete comparison execution result containing the shared baseline and all mode-specific runs.
        /// </returns>
        private static ExceptionComparisonExecutionResult RunProjectOrSolutionComparison(
            string path,
            ToolOptions options)
        {
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }

            ConsoleLogger.Info("Project/solution detected. Running comparison with semantic analysis.");
            Stopwatch totalStopwatch = Stopwatch.StartNew();

            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            IProgress<ProjectLoadProgress> progress = new Progress<ProjectLoadProgress>(p =>
            {
                ConsoleLogger.InfoProgress($"[MSBuild] {p.Operation} - {p.FilePath}");
            });

            List<Project> projectsToAnalyze = LoadProjectsToAnalyze(path, options, workspace, progress);

            Stopwatch sharedStopwatch = Stopwatch.StartNew();
            PreparedSemanticComparisonInput prepared = PrepareSemanticComparisonInput(projectsToAnalyze, options);
            sharedStopwatch.Stop();

            ExceptionComparisonExecutionResult comparisonResult = new()
            {
                SharedBaselineResult = prepared.BaselineResult.Clone(),
                SharedDetectorsDurationMs = sharedStopwatch.ElapsedMilliseconds
            };

            foreach (ExceptionAnalysisMode mode in Enum.GetValues<ExceptionAnalysisMode>())
            {
                ToolOptions modeOptions = CreateModeSpecificOptions(options, mode);
                ExceptionModeExecutionResult modeExecution =
                    ExecuteModeSpecificExceptionRun(prepared, modeOptions, mode, comparisonResult.SharedDetectorsDurationMs);

                comparisonResult.Modes.Add(modeExecution);
            }

            totalStopwatch.Stop();
            ConsoleLogger.Info($"\nComparison analysis finished in {totalStopwatch.ElapsedMilliseconds} ms.");

            return comparisonResult;
        }

        /// <summary>
        /// Prepares the shared semantic baseline for comparison mode.
        /// </summary>
        /// <param name="projectsToAnalyze">The reporting projects to analyze.</param>
        /// <param name="options">The base tool options.</param>
        /// <returns>
        /// A prepared comparison input containing the shared baseline findings, semantic models,
        /// and per-file prepared documents.
        /// </returns>
        private static PreparedSemanticComparisonInput PrepareSemanticComparisonInput(
            List<Project> projectsToAnalyze,
            ToolOptions options)
        {
            PreparedSemanticComparisonInput prepared = new();
            prepared.Projects.AddRange(projectsToAnalyze);

            int totalDocuments = projectsToAnalyze.Sum(p => p.Documents.Count());
            int currentDocument = 0;

            foreach (Project project in projectsToAnalyze)
            {
                NamespaceDocumentationAggregator namespaceAggregator =
                    new(options.XmlDocOptions.RequireDocumentationForNamespaces);

                if (projectsToAnalyze.Count > 1)
                {
                    ConsoleLogger.Info($"Preparing baseline for project: {project.Name}");
                }

                Compilation? compilation = project.GetCompilationAsync().GetAwaiter().GetResult();
                if (compilation == null)
                {
                    ConsoleLogger.Warn($"Compilation for project {project.Name} is null. Skipping this project.");
                    continue;
                }

                foreach (Document document in project.Documents)
                {
                    currentDocument++;
                    ConsoleLogger.InfoVerbose($"[baseline {currentDocument}/{totalDocuments}] {document.Name}");

                    string? filePath = document.FilePath;
                    if (filePath == null)
                    {
                        continue;
                    }

                    if (ToolFileFilter.ShouldExclude(filePath, options))
                    {
                        continue;
                    }

                    SyntaxTree? tree = document.GetSyntaxTreeAsync().GetAwaiter().GetResult();
                    if (tree == null)
                    {
                        continue;
                    }

                    AccumulateSloc(prepared.BaselineResult, tree, filePath, options);
                    IReadOnlyDictionary<string, int> fileTotals = DocumentationStatisticsCollector.Collect(tree);
                    prepared.BaselineResult.AccumulateTotals(fileTotals);

                    SemanticModel semanticModel = compilation.GetSemanticModel(tree);

                    List<Finding> baselineFindings =
                        CollectSharedSemanticBaselineFindings(
                            tree,
                            filePath,
                            semanticModel,
                            options,
                            namespaceAggregator);

                    prepared.BaselineResult.AccumulateFindings(baselineFindings);
                    prepared.BaselineFindingsByFile[filePath] = baselineFindings;

                    prepared.Documents.Add(new PreparedSemanticDocument
                    {
                        FilePath = filePath,
                        Tree = tree,
                        SemanticModel = semanticModel
                    });
                }

                List<Finding> namespaceFindings =
                    namespaceAggregator.CreateMissingCentralNamespaceFindings();

                prepared.BaselineResult.AccumulateTotals(new Dictionary<string, int>(StringComparer.Ordinal)
                {
                    {
                        StatisticsKeys.UniqueNamespacesTotal, namespaceAggregator.UniqueNamespaceKeyCount
                    }
                });

                foreach (IGrouping<string, Finding> group in namespaceFindings.GroupBy(f => f.FilePath))
                {
                    if (!prepared.BaselineFindingsByFile.TryGetValue(group.Key, out List<Finding>? existing))
                    {
                        existing = new List<Finding>();
                        prepared.BaselineFindingsByFile[group.Key] = existing;
                    }

                    existing.AddRange(group);
                    prepared.BaselineResult.AccumulateFindings(group.ToList());
                }
            }

            return prepared;
        }

        /// <summary>
        /// Collects all baseline findings that do not depend on the configured exception analysis mode.
        /// </summary>
        /// <param name="tree">The syntax tree to analyze.</param>
        /// <param name="filePath">The file path used for reporting.</param>
        /// <param name="semanticModel">The semantic model.</param>
        /// <param name="options">The tool options.</param>
        /// <param name="namespaceAggregator">The namespace documentation aggregator.</param>
        /// <returns>The collected baseline findings.</returns>
        private static List<Finding> CollectSharedSemanticBaselineFindings(
            SyntaxTree tree,
            string filePath,
            SemanticModel semanticModel,
            ToolOptions options,
            NamespaceDocumentationAggregator namespaceAggregator)
        {
            List<Finding> findings =
                new(XmlDocBasicDetector.FindBasicSmells(tree, filePath, options.XmlDocOptions, namespaceAggregator));

            foreach (XmlDocDetectorCatalog.SyntaxDetector detector in XmlDocDetectorCatalog.SyntaxDetectors)
            {
                findings.AddRange(detector(tree, filePath));
            }

            foreach (XmlDocDetectorCatalog.SemanticDetector detector in XmlDocDetectorCatalog.SemanticDetectors)
            {
                findings.AddRange(detector(tree, filePath, semanticModel));
            }

            return findings;
        }

        /// <summary>
        /// Executes one mode-specific exception analysis run on top of the shared baseline.
        /// </summary>
        /// <param name="prepared">The prepared shared comparison input.</param>
        /// <param name="modeOptions">The mode-specific tool options.</param>
        /// <param name="mode">The executed exception analysis mode.</param>
        /// <returns>
        /// The internal mode execution result containing the combined findings, report path, and duration.
        /// </returns>
        private static ExceptionModeExecutionResult ExecuteModeSpecificExceptionRun(
     PreparedSemanticComparisonInput prepared,
     ToolOptions modeOptions,
     ExceptionAnalysisMode mode,
     long sharedDetectorsDurationMs)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            RunResult modeResult = prepared.BaselineResult.Clone();
            IFindingsReporter reporter = FindingsReporterFactory.Create(modeOptions);

            ProjectClosureSemanticContext semanticContext =
                ProjectClosureSemanticContextBuilder.Build(
                    prepared.Projects,
                    modeOptions.XmlDocOptions.ExceptionAnalysisMode);

            foreach (PreparedSemanticDocument document in prepared.Documents)
            {
                List<Finding> combinedFindings = new();

                if (prepared.BaselineFindingsByFile.TryGetValue(document.FilePath, out List<Finding>? baselineFindings))
                {
                    combinedFindings.AddRange(baselineFindings);
                }

                List<Finding> exceptionFindings =
                    XmlDocExceptionSemanticDetector.FindExceptionSmells(
                        document.Tree,
                        document.FilePath,
                        document.SemanticModel,
                        semanticContext,
                        modeOptions.XmlDocOptions);

                if (exceptionFindings.Count > 0)
                {
                    modeResult.AccumulateFindings(exceptionFindings);
                    combinedFindings.AddRange(exceptionFindings);
                }

                reporter.ReportFile(document.FilePath, combinedFindings);
            }

            stopwatch.Stop();
            modeResult.AnalysisDurationMs =
                sharedDetectorsDurationMs + stopwatch.ElapsedMilliseconds;

            CompleteReporting(reporter, modeResult);

            return new ExceptionModeExecutionResult
            {
                Mode = mode,
                Result = modeResult,
                ReportPath = modeOptions.OutputPath,
                ExceptionDetectorDurationMs = stopwatch.ElapsedMilliseconds
            };
        }

        /// <summary>
        /// Creates a cloned tool options instance for one exception analysis mode.
        /// </summary>
        /// <param name="baseOptions">The base comparison options.</param>
        /// <param name="mode">The target exception analysis mode.</param>
        /// <returns>
        /// A new <see cref="ToolOptions"/> instance for the specified mode.
        /// </returns>
        private static ToolOptions CreateModeSpecificOptions(
            ToolOptions baseOptions,
            ExceptionAnalysisMode mode)
        {
            XmlDocOptions xmlDocOptions = new()
            {
                CheckEnumMembers = baseOptions.XmlDocOptions.CheckEnumMembers,
                RequireSummaryForFields = baseOptions.XmlDocOptions.RequireSummaryForFields,
                RequireDocumentationForNamespaces = baseOptions.XmlDocOptions.RequireDocumentationForNamespaces,
                ExceptionAnalysisMode = mode
            };

            string? reportPath = ResolveModeReportPath(baseOptions, mode);

            return new ToolOptions(
                targetPath: baseOptions.TargetPath,
                checkOnly: baseOptions.CheckOnly,
                cleanBackups: false,
                useTest: false,
                xmlDocOptions: xmlDocOptions,
                outputFormat: baseOptions.OutputFormat,
                outputPath: reportPath,
                verbose: baseOptions.Verbose,
                fullAnalysis: baseOptions.FullAnalysis,
                projectName: baseOptions.ProjectName,
                includeGenerated: baseOptions.IncludeGenerated,
                includeTests: baseOptions.IncludeTests,
                compareExceptionAnalysisModes: false);
        }

        /// <summary>
        /// Resolves the per-mode report path for JSON or SARIF comparison outputs.
        /// </summary>
        /// <param name="baseOptions">The base comparison options.</param>
        /// <param name="mode">The target exception analysis mode.</param>
        /// <returns>
        /// The per-mode report path, or <see langword="null"/> for console-only output.
        /// </returns>
        private static string? ResolveModeReportPath(
            ToolOptions baseOptions,
            ExceptionAnalysisMode mode)
        {
            if (baseOptions.OutputFormat == OutputFormat.Console)
            {
                return null;
            }

            string basePath = baseOptions.OutputPath ?? GetDefaultReportPath(baseOptions.OutputFormat);
            string directory = Path.GetDirectoryName(basePath) ?? string.Empty;
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(basePath);
            string extension = Path.GetExtension(basePath);

            fileNameWithoutExtension = RemoveKnownModeSuffix(fileNameWithoutExtension);

            string modeSuffix = mode switch
            {
                ExceptionAnalysisMode.Direct => "direct",
                ExceptionAnalysisMode.ProjectTransitive => "project-transitive",
                ExceptionAnalysisMode.ProjectTransitiveProjectExceptions => "project-transitive-project-exceptions",
                ExceptionAnalysisMode.SolutionTransitive => "solution-transitive",
                _ => mode.ToString().ToLowerInvariant()
            };

            string fileName = $"{fileNameWithoutExtension}_{modeSuffix}{extension}";
            return string.IsNullOrWhiteSpace(directory)
                ? fileName
                : Path.Combine(directory, fileName);
        }

        /// <summary>
        /// Removes one known exception-mode suffix from a file name without extension.
        /// </summary>
        /// <param name="fileNameWithoutExtension">The file name without extension.</param>
        /// <returns>The normalized base file name.</returns>
        private static string RemoveKnownModeSuffix(string fileNameWithoutExtension)
        {
            string[] suffixes =
            [
                "_direct",
                "_project-transitive",
                "_project-transitive-project-exceptions",
                "_solution-transitive"
            ];

            foreach (string suffix in suffixes)
            {
                if (fileNameWithoutExtension.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return fileNameWithoutExtension[..^suffix.Length];
                }
            }

            return fileNameWithoutExtension;
        }

        /// <summary>
        /// Returns a default report path for machine-readable output formats.
        /// </summary>
        /// <param name="format">The selected output format.</param>
        /// <returns>The default report path.</returns>
        private static string GetDefaultReportPath(OutputFormat format)
        {
            return format switch
            {
                OutputFormat.Json => "artifacts/findings.json",
                OutputFormat.Sarif => "artifacts/findings.sarif",
                _ => "artifacts/findings.txt"
            };
        }

        /// <summary>
        /// Runs the tool in check-only mode on plain files or directories without semantic project loading.
        /// </summary>
        /// <param name="files">The C# files to analyze.</param>
        /// <param name="options">The active tool options.</param>
        /// <returns>
        /// The aggregated run result including findings, totals, SLOC, and analysis duration.
        /// </returns>
        private static RunResult RunCheck(List<string> files, ToolOptions options)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            IFindingsReporter reporter = FindingsReporterFactory.Create(options);
            RunResult result = new();

            NamespaceDocumentationAggregator namespaceAggregator =
                new(options.XmlDocOptions.RequireDocumentationForNamespaces);

            foreach (string file in files)
            {
                if (ToolFileFilter.ShouldExclude(file, options))
                {
                    continue;
                }

                string text = FileText.ReadAllTextPreserveEncoding(file, out Encoding encoding, out bool hasBom);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(text, path: file);

                AccumulateSloc(result, tree, file, options);
                IReadOnlyDictionary<string, int> fileTotals = DocumentationStatisticsCollector.Collect(tree);
                result.AccumulateTotals(fileTotals);

                List<Finding> findings = new(XmlDocBasicDetector.FindBasicSmells(tree, file, options.XmlDocOptions, namespaceAggregator));

                foreach (XmlDocDetectorCatalog.SyntaxDetector detector in XmlDocDetectorCatalog.SyntaxDetectors)
                {
                    findings.AddRange(detector(tree, file));
                }

                result.AccumulateFindings(findings);
                reporter.ReportFile(file, findings);
            }

            FlushNamespaceFindings(namespaceAggregator, result, reporter);

            stopwatch.Stop();
            result.AnalysisDurationMs = stopwatch.ElapsedMilliseconds;
            CompleteReporting(reporter, result);

            return result;
        }

        /// <summary>
        /// Runs the tool in fix mode on plain files or directories without semantic project loading.
        /// </summary>
        /// <param name="files">The C# files to process.</param>
        /// <param name="options">The active tool options.</param>
        /// <returns>
        /// The aggregated run result including totals, changed files, SLOC, and analysis duration.
        /// </returns>
        private static RunResult RunFix(List<string> files, ToolOptions options)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            RunResult result = new();

            foreach (string originalFile in files)
            {
                FixSingleFile(originalFile, options, result);
            }

            stopwatch.Stop();
            result.AnalysisDurationMs = stopwatch.ElapsedMilliseconds;

            return result;
        }

        /// <summary>
        /// Fixes a single file, optionally using a .bak copy in test mode.
        /// </summary>
        /// <param name="originalFile">Original file path.</param>
        /// <param name="options">Tool options.</param>
        /// <param name="result">Accumulated run result.</param>
        private static void FixSingleFile(string originalFile, ToolOptions options, RunResult result)
        {
            if (ToolFileFilter.ShouldExclude(originalFile, options))
            {
                return;
            }

            string file = originalFile;
            string? backupPath = null;

            if (options.UseTest)
            {
                backupPath = BackupManager.CreateBackup(originalFile);
                file = backupPath;
            }

            string text = FileText.ReadAllTextPreserveEncoding(file, out Encoding encoding, out bool hasBom);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(text, path: file);

            AccumulateSloc(result, tree, originalFile, options);
            IReadOnlyDictionary<string, int> fileTotals = DocumentationStatisticsCollector.Collect(tree);
            result.AccumulateTotals(fileTotals);

            List<Finding> malformedFindings = XmlDocWellFormedDetector.FindMalformedTags(tree, file);
            if (malformedFindings.Count > 0)
            {
                result.AccumulateFindings(malformedFindings);
                ConsoleReporter.PrintFindings(file, malformedFindings);

                DeleteBackupOnAbort(backupPath);
                return;
            }

            SyntaxNode root = tree.GetRoot();
            SyntaxNode afterLiteralFix = new LiteralRefactorer().Visit(root);
            SyntaxNode afterDocFix = new XmlDocRewriter().Visit(afterLiteralFix);

            if (!ReferenceEquals(root, afterDocFix))
            {
                FileText.WriteAllTextPreserveEncoding(file, afterDocFix.ToFullString(), encoding, hasBom);
                result.ChangedFiles++;

                ConsoleLogger.Info(options.UseTest ? $"Fixed (backup): {file}" : $"Fixed: {file}");
            }
        }

        /// <summary>
        /// Deletes a backup file if it exists.
        /// </summary>
        /// <param name="backupPath">Backup path or null.</param>
        private static void DeleteBackupOnAbort(string? backupPath)
        {
            if (backupPath == null)
            {
                return;
            }

            if (!File.Exists(backupPath))
            {
                return;
            }

            File.Delete(backupPath);
            ConsoleLogger.Info($"Deleted backup: {backupPath}");
        }

        /// <summary>
        /// Adds SLOC for the given syntax tree to the run result if the file is not excluded.
        /// </summary>
        /// <param name="result">The aggregated run result.</param>
        /// <param name="tree">The syntax tree to count.</param>
        /// <param name="filePath">The file path used for filtering.</param>
        /// <param name="options">The tool options controlling inclusion behavior.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="result"/>, <paramref name="tree"/>, 
        /// <paramref name="filePath"/> or <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        private static void AccumulateSloc(
            RunResult result,
            SyntaxTree tree,
            string filePath,
            ToolOptions options)
        {
            ArgumentNullException.ThrowIfNull(result);
            ArgumentNullException.ThrowIfNull(tree);
            ArgumentNullException.ThrowIfNull(filePath);
            ArgumentNullException.ThrowIfNull(options);

            result.Sloc += SlocCalculator.CalculateForTree(tree);
        }

        /// <summary>
        /// Emits aggregated namespace findings (DOC101) collected by the namespace documentation aggregator.
        /// </summary>
        /// <param name="namespaceAggregator">The namespace documentation aggregator.</param>
        /// <param name="result">The run result to accumulate findings into.</param>
        /// <param name="reporter">The reporter used to output findings.</param>
        private static void FlushNamespaceFindings(
            NamespaceDocumentationAggregator namespaceAggregator,
            RunResult result,
            IFindingsReporter reporter)
        {
            result.AccumulateTotals(new Dictionary<string, int>(StringComparer.Ordinal)
            {
                {
                    StatisticsKeys.UniqueNamespacesTotal, namespaceAggregator.UniqueNamespaceKeyCount
                }
            });

            List<Finding> namespaceFindings =
                namespaceAggregator.CreateMissingCentralNamespaceFindings();

            if (namespaceFindings.Count <= 0)
            {
                return;
            }

            IEnumerable<IGrouping<string, Finding>> grouped =
                namespaceFindings.GroupBy(f => f.FilePath);

            foreach (IGrouping<string, Finding> group in grouped)
            {
                List<Finding> groupFindings = group.ToList();
                result.AccumulateFindings(groupFindings);
                reporter.ReportFile(group.Key, groupFindings);
            }
        }

        /// <summary>
        /// Completes reporting and passes the <see cref="RunResult"/> to result-aware reporters.
        /// </summary>
        /// <param name="reporter">The reporter instance.</param>
        /// <param name="result">The aggregated run result.</param>
        private static void CompleteReporting(IFindingsReporter reporter, RunResult result)
        {
            if (reporter is IResultAwareFindingsReporter resultAware)
            {
                resultAware.Complete(result);
                return;
            }

            reporter.Complete();
        }

        /// <summary>
        /// Stores one prepared semantic document for comparison runs.
        /// </summary>
        /// <remarks>
        /// The shared baseline preparation resolves syntax trees and semantic models once and keeps
        /// them here so that the mode-specific exception analysis can reuse them without rebuilding
        /// the complete non-exception pipeline.
        /// </remarks>
        private sealed class PreparedSemanticDocument
        {
            /// <summary>
            /// Gets or sets the file path.
            /// </summary>
            public string FilePath { get; set; } = string.Empty;

            /// <summary>
            /// Gets or sets the syntax tree.
            /// </summary>
            public SyntaxTree Tree { get; set; } = null!;

            /// <summary>
            /// Gets or sets the semantic model.
            /// </summary>
            public SemanticModel SemanticModel { get; set; } = null!;
        }

        /// <summary>
        /// Stores the prepared shared baseline for a semantic comparison run.
        /// </summary>
        /// <remarks>
        /// This object contains all data that is independent from the selected exception analysis mode:
        /// reporting projects, prepared documents, shared baseline findings, and the aggregated baseline result.
        /// </remarks>
        private sealed class PreparedSemanticComparisonInput
        {
            /// <summary>
            /// Gets the reporting projects.
            /// </summary>
            public List<Project> Projects { get; } = new();

            /// <summary>
            /// Gets the prepared documents.
            /// </summary>
            public List<PreparedSemanticDocument> Documents { get; } = new();

            /// <summary>
            /// Gets the shared baseline result that contains all non-mode-dependent findings.
            /// </summary>
            public RunResult BaselineResult { get; } = new();

            /// <summary>
            /// Gets the shared baseline findings per file.
            /// </summary>
            public Dictionary<string, List<Finding>> BaselineFindingsByFile { get; } =
                new(StringComparer.Ordinal);
        }
    }
}
