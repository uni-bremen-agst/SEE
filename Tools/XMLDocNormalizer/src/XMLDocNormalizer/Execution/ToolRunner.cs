using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Cli;
using XMLDocNormalizer.IO;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Reporting;
using XMLDocNormalizer.Reporting.Abstractions;
using XMLDocNormalizer.Reporting.Console;
using XMLDocNormalizer.Reporting.Logging;
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
            Logger.VerboseEnabled = options.Verbose;
            Logger.Info($"Running XMLDocNormalizer with target: {options.TargetPath}");

            if (!File.Exists(options.TargetPath) && !Directory.Exists(options.TargetPath))
            {
                throw new ArgumentException($"Invalid target path: {options.TargetPath}", nameof(options));
            }

            if (options.TargetPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                options.TargetPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Info("Project/solution detected. Running with semantic analysis.");
                return RunProjectOrSolution(options.TargetPath, options);
            }

            Logger.Info("File or directory detected. Running without semantic analysis.");
            List<string> files = FileDiscovery.EnumerateCsFiles(options.TargetPath);

            if (options.CheckOnly)
            {
                Logger.Info("Check-only mode enabled. No changes will be made.");
                return RunCheck(files, options);
            }

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

            Logger.Info($"Opening: {path}");
            Stopwatch stopwatch = Stopwatch.StartNew();

            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            IProgress<ProjectLoadProgress> progress = new Progress<ProjectLoadProgress>(p =>
            {
                Logger.Info($"[MSBuild] {p.Operation} - {p.FilePath}");
            });

            RunResult result = new();
            IFindingsReporter reporter = FindingsReporterFactory.Create(options);
            List<Project> projectsToAnalyze = new();

            if (path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                // Single project file
                Project project = workspace.OpenProjectAsync(path, progress)
                                           .GetAwaiter()
                                           .GetResult();
                projectsToAnalyze.Add(project);
                Logger.Info($"Analyzing single project: {project.Name}");
            }
            else if (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                // Solution file
                Solution solution = workspace.OpenSolutionAsync(path, progress)
                                             .GetAwaiter()
                                             .GetResult();
                Logger.Info($"Loaded {solution.Projects.Count()} project(s) in solution.");

                if (options.FullAnalysis)
                {
                    projectsToAnalyze.AddRange(solution.Projects);
                    Logger.Info("Full analysis enabled: all projects in solution will be analyzed.");
                }
                else if (!string.IsNullOrWhiteSpace(options.ProjectName))
                {
                    // Analyze project specified via --project
                    Project? project = solution.Projects.FirstOrDefault(p =>
                        string.Equals(p.Name, options.ProjectName, StringComparison.OrdinalIgnoreCase));

                    if (project == null)
                    {
                        throw new ArgumentException(
                            $"Project '{options.ProjectName}' was not found in solution '{solution.FilePath}'.");
                    }

                    projectsToAnalyze.Add(project);
                    Logger.Info($"Analyzing project: {project.Name}");
                }
                else
                {
                    // Default: try to pick project matching solution name
                    string solutionName = Path.GetFileNameWithoutExtension(path);
                    Project? project = solution.Projects.FirstOrDefault(p =>
                        string.Equals(p.Name, solutionName, StringComparison.OrdinalIgnoreCase));

                    if (project != null)
                    {
                        projectsToAnalyze.Add(project);
                        Logger.Info($"Analyzing project matching solution name: {project.Name}");
                    }
                    else
                    {
                        projectsToAnalyze.Add(solution.Projects.First());
                        Logger.Warn($"No project matching solution name '{solutionName}' found. Analyzing first project: {projectsToAnalyze[0].Name}");
                    }
                }
            }
            else
            {
                throw new ArgumentException($"Invalid target path: {path}. Must be .csproj or .sln.", nameof(path));
            }

            // Count total documents
            int totalDocuments = projectsToAnalyze.Sum(p => p.Documents.Count());
            Logger.Info($"Processing {totalDocuments} document(s)...");

            int currentDocument = 0;
            foreach (Project project in projectsToAnalyze)
            {
                Logger.Info($"Project: {project.Name}");
                Compilation? compilation = project.GetCompilationAsync().GetAwaiter().GetResult();

                if (compilation == null)
                {
                    Logger.Warn($"Compilation for project {project.Name} is null. Skipping this project.");
                    continue;
                }

                foreach (Document document in project.Documents)
                {
                    currentDocument++;
                    Logger.Info($"[{currentDocument}/{totalDocuments}] {document.Name}");

                    string? filePath = document.FilePath;
                    if (filePath == null) continue;

                    SyntaxTree? syntaxTree = document.GetSyntaxTreeAsync().GetAwaiter().GetResult();
                    if (syntaxTree == null) continue;

                    SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);

                    List<Finding> findings = XmlDocWellFormedDetector.FindMalformedTags(syntaxTree, filePath);
                    findings.AddRange(XmlDocBasicDetector.FindBasicSmells(syntaxTree, filePath, options.XmlDocOptions));
                    findings.AddRange(XmlDocParamDetector.FindParamSmells(syntaxTree, filePath));
                    findings.AddRange(XmlDocTypeParamDetector.FindTypeParamSmells(syntaxTree, filePath));
                    findings.AddRange(XmlDocReturnsDetector.FindReturnsSmells(syntaxTree, filePath));
                    findings.AddRange(XmlDocExceptionDetector.FindExceptionSmells(syntaxTree, filePath));

                    if (findings.Count > 0)
                        result.FindingCount += findings.Count;

                    reporter.ReportFile(filePath, findings);
                }
            }

            reporter.Complete();
            stopwatch.Stop();
            Logger.Info($"Semantic analysis finished in {stopwatch.ElapsedMilliseconds} ms.");
            return result;
        }

        /// <summary>
        /// Runs the tool in check-only mode.
        /// </summary>
        /// <param name="files">List of C# files.</param>
        /// <param name="options">Tool options.</param>
        /// <returns>The aggregated run result.</returns>
        private static RunResult RunCheck(List<string> files, ToolOptions options)
        {
            IFindingsReporter reporter = FindingsReporterFactory.Create(options);
            RunResult result = new();

            foreach (string file in files)
            {
                string text = FileText.ReadAllTextPreserveEncoding(file, out Encoding encoding, out bool hasBom);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(text);

                List<Finding> findings = XmlDocWellFormedDetector.FindMalformedTags(tree, file);
                findings.AddRange(XmlDocBasicDetector.FindBasicSmells(tree, file, options.XmlDocOptions));
                findings.AddRange(XmlDocParamDetector.FindParamSmells(tree, file));
                findings.AddRange(XmlDocTypeParamDetector.FindTypeParamSmells(tree, file));
                findings.AddRange(XmlDocReturnsDetector.FindReturnsSmells(tree, file));
                findings.AddRange(XmlDocExceptionDetector.FindExceptionSmells(tree, file));

                if (findings.Count > 0)
                {
                    result.FindingCount += findings.Count;
                }

                reporter.ReportFile(file, findings);
            }

            reporter.Complete();
            return result;
        }

        /// <summary>
        /// Runs the tool in fix mode.
        /// </summary>
        /// <param name="files">List of C# files.</param>
        /// <param name="options">Tool options.</param>
        /// <returns>The aggregated run result.</returns>
        private static RunResult RunFix(List<string> files, ToolOptions options)
        {
            RunResult result = new();

            foreach (string originalFile in files)
            {
                FixSingleFile(originalFile, options, result);
            }

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
            string file = originalFile;
            string? backupPath = null;

            if (options.UseTest)
            {
                backupPath = BackupManager.CreateBackup(originalFile);
                file = backupPath;
            }

            string text = FileText.ReadAllTextPreserveEncoding(file, out Encoding encoding, out bool hasBom);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(text);

            List<Finding> malformed = XmlDocWellFormedDetector.FindMalformedTags(tree, file);
            if (malformed.Count > 0)
            {
                result.FindingCount += malformed.Count;
                ConsoleReporter.PrintFindings(file, malformed);

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

                Console.WriteLine(options.UseTest ? $"Fixed (backup): {file}" : $"Fixed: {file}");
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
            Console.WriteLine($"Deleted backup: {backupPath}");
        }
    }
}
