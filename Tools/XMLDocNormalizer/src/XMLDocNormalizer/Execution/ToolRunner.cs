using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Cli;
using XMLDocNormalizer.Configuration;
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
            List<string> files = FileDiscovery.EnumerateCsFiles(options.TargetPath);

            if (options.CheckOnly)
            {
                return RunCheck(files, options);
            }

            return RunFix(files, options);
        }

        /// <summary>
        /// Runs the tool in check-only mode.
        /// </summary>
        /// <param name="files">The files to check.</param>
        /// <param name="xmlDocOptions">The XML documation options.</param>
        /// <returns>The aggregated run result.</returns>
        private static RunResult RunCheck(List<string> files, ToolOptions options)
        {
            IFindingsReporter reporter = FindingsReporterFactory.Create(options);
            RunResult result = new();

            foreach (string file in files)
            {
                string text = FileText.ReadAllTextPreserveEncoding(file, out Encoding _, out bool _);
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
        /// <param name="files">The files to fix.</param>
        /// <param name="options">The tool options.</param>
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
        /// Fixes a single file, optionally using a <c>.bak</c> copy in test mode.
        /// </summary>
        /// <param name="originalFile">The original file path.</param>
        /// <param name="options">The tool options.</param>
        /// <param name="result">The run result accumulator.</param>
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
        /// This is used when a test-mode rewrite is aborted due to malformed XML documentation.
        /// </summary>
        /// <param name="backupPath">The backup path to delete, or <c>null</c>.</param>
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
