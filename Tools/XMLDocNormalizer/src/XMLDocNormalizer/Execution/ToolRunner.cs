using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Configuration;
using XMLDocNormalizer.Cli;
using XMLDocNormalizer.IO;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Reporting;
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
                return RunCheck(files, options.XmlDocOptions);
            }

            return RunFix(files, options.UseTest);
        }

        /// <summary>
        /// Runs the tool in check-only mode.
        /// </summary>
        /// <param name="files">The files to check.</param>
        /// <param name="xmlDocOptions">The XML documation options.</param>
        /// <returns>The aggregated run result.</returns>
        private static RunResult RunCheck(List<string> files, XmlDocOptions xmlDocOptions)
        {
            RunResult result = new();

            foreach (string file in files)
            {
                string text = FileText.ReadAllTextPreserveEncoding(file, out Encoding _, out bool _);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(text);

                List<Finding> findings = XmlDocWellFormedDetector.FindMalformedTags(tree, file);
                findings.AddRange(XmlDocBasicDetector.FindBasicSmells(tree, file, xmlDocOptions));
                findings.AddRange(XmlDocParamDetector.FindParamSmells(tree, file));
                findings.AddRange(XmlDocTypeParamDetector.FindTypeParamSmells(tree, file));

                if (findings.Count > 0)
                {
                    result.FindingCount += findings.Count;
                    ConsoleReporter.PrintFindings(file, findings);
                }
            }

            return result;
        }

        /// <summary>
        /// Runs the tool in fix mode.
        /// </summary>
        /// <param name="files">The files to fix.</param>
        /// <param name="useTest">True to rewrite only timestamped <c>.bak</c> copies.</param>
        /// <returns>The aggregated run result.</returns>
        private static RunResult RunFix(List<string> files, bool useTest)
        {
            RunResult result = new();

            foreach (string originalFile in files)
            {
                FixSingleFile(originalFile, useTest, result);
            }

            return result;
        }

        /// <summary>
        /// Fixes a single file, optionally using a <c>.bak</c> copy in test mode.
        /// </summary>
        /// <param name="originalFile">The original file path.</param>
        /// <param name="useTest">True to rewrite a <c>.bak</c> file only.</param>
        /// <param name="result">The run result accumulator.</param>
        private static void FixSingleFile(string originalFile, bool useTest, RunResult result)
        {
            string inputPath = originalFile;
            string? backupPath = null;

            if (useTest)
            {
                backupPath = BackupManager.CreateBackup(originalFile);
                inputPath = backupPath;
            }

            string text = FileText.ReadAllTextPreserveEncoding(inputPath, out Encoding encoding, out bool hasBom);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(text);

            List<Finding> malformed = XmlDocWellFormedDetector.FindMalformedTags(tree, inputPath);
            if (malformed.Count > 0)
            {
                result.FindingCount += malformed.Count;
                ConsoleReporter.PrintFindings(inputPath, malformed);

                DeleteBackupOnAbort(backupPath);
                return;
            }

            SyntaxNode root = tree.GetRoot();
            SyntaxNode afterLiteralFix = new LiteralRefactorer().Visit(root);
            SyntaxNode afterDocFix = new XmlDocRewriter().Visit(afterLiteralFix);

            if (!ReferenceEquals(root, afterDocFix))
            {
                FileText.WriteAllTextPreserveEncoding(inputPath, afterDocFix.ToFullString(), encoding, hasBom);
                result.ChangedFiles++;

                Console.WriteLine(useTest ? $"Fixed (backup): {inputPath}" : $"Fixed: {inputPath}");
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
