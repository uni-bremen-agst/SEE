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
using XMLDocNormalizer.Rewriting;

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
            Console.WriteLine($"Running XMLDocNormalizer with target: {options.TargetPath}");

            if (!File.Exists(options.TargetPath) && !Directory.Exists(options.TargetPath))
            {
                throw new ArgumentException($"Invalid target path: {options.TargetPath}", nameof(options));
            }

            if (options.TargetPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ||
                options.TargetPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Project/solution detected. Running with semantic analysis.");
                return RunProjectOrSolution(options.TargetPath, options);
            }

            Console.WriteLine("File or directory detected. Running without semantic analysis.");
            List<string> files = FileDiscovery.EnumerateCsFiles(options.TargetPath);

            if (options.CheckOnly)
            {
                Console.WriteLine("Check-only mode enabled. No changes will be made.");
                return RunCheck(files, options);
            }

            return RunFix(files, options);
        }


        /// <summary>
        /// Runs the tool on a C# project or solution using semantic analysis.
        /// </summary>
        /// <param name="path">Path to the .csproj or .sln file.</param>
        /// <param name="options">Tool options.</param>
        /// <returns>The aggregated run result.</returns>
        private static RunResult RunProjectOrSolution(string path, ToolOptions options)
        {
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }

            MSBuildWorkspace workspace = MSBuildWorkspace.Create();

            Solution solution;
            if (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                solution = workspace.OpenSolutionAsync(path).Result;
            }
            else
            {
                Project project = workspace.OpenProjectAsync(path).Result;
                solution = project.Solution;
            }

            RunResult result = new();
            IFindingsReporter reporter = FindingsReporterFactory.Create(options);

            foreach (Project project in solution.Projects)
            {
                foreach (Document document in project.Documents)
                {
                    string text = document.GetTextAsync().Result.ToString();
                    SyntaxTree tree = CSharpSyntaxTree.ParseText(text);

                    string? filePath = document.FilePath;
                    if (filePath != null)
                    {
                        List<Finding> findings = XmlDocWellFormedDetector.FindMalformedTags(tree, filePath);
                        findings.AddRange(XmlDocBasicDetector.FindBasicSmells(tree, filePath, options.XmlDocOptions));
                        findings.AddRange(XmlDocParamDetector.FindParamSmells(tree, filePath));
                        findings.AddRange(XmlDocTypeParamDetector.FindTypeParamSmells(tree, filePath));
                        findings.AddRange(XmlDocReturnsDetector.FindReturnsSmells(tree, filePath));
                        findings.AddRange(XmlDocExceptionDetector.FindExceptionSmells(tree, filePath));

                        if (findings.Count > 0)
                        {
                            result.FindingCount += findings.Count;
                        }

                        reporter.ReportFile(filePath, findings);
                    }
                }
            }

            reporter.Complete();
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
            RunResult result = new RunResult();

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
            RunResult result = new RunResult();

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
