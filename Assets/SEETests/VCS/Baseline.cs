using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SEE.VCS
{
    /// <summary>
    /// The report of the previous run of a test case, kept so that the next
    /// run can be held against it. Its purpose is to show that a change to
    /// the code producing the report leaves the report itself alone.
    /// </summary>
    /// <remarks>
    /// Mind that the report depends on the history of the repository as much
    /// as on the code deriving it. Every commit added to a branch reported on
    /// may legitimately change it, so a difference is not by itself a defect.
    /// The tips recorded beside the report say which of the two it is: same
    /// tips and a differing report means the code changed its answer.
    ///
    /// A baseline is kept outside the repository, under the temporary
    /// directory, as the other tests here keep what they write. To accept the
    /// report of the latest run as the one to come, delete the file; the next
    /// run writes it afresh.
    /// </remarks>
    internal static class Baseline
    {
        /// <summary>
        /// Separates the circumstances of a run from its report. Only what
        /// follows it is compared.
        /// </summary>
        private const string marker = "----- report -----";

        /// <summary>
        /// The directory the baselines are kept in.
        /// </summary>
        internal static string Directory
            => Path.Combine(Path.GetTempPath(), "SEE", "Baselines");

        /// <summary>
        /// Holds <paramref name="report"/> against the baseline of the test
        /// case named <paramref name="testCase"/>, failing the test where the
        /// two differ. Where there is no baseline yet, writes one.
        /// </summary>
        /// <param name="testCase">The name of the test case the report belongs to.</param>
        /// <param name="context">The circumstances the report was produced under. Recorded
        /// beside it and not compared. A line reading <c>tip &lt;name&gt; &lt;sha&gt;</c> is
        /// read back from it, so that a report differing because the repository has moved on
        /// can be told from one differing because the code deriving it has.</param>
        /// <param name="report">What is held against the baseline.</param>
        internal static void CompareOrWrite(string testCase, string context, string report)
        {
            string file = Path.Combine(Directory, FilenameOf(testCase) + ".txt");
            if (!File.Exists(file))
            {
                System.IO.Directory.CreateDirectory(Directory);
                File.WriteAllText(file, Content(context, report));
                Debug.Log($"No baseline yet; wrote one to {file}.\n");
                return;
            }

            string[] stored = Lines(Payload(File.ReadAllText(file)));
            string[] produced = Lines(report);
            int line = FirstDifference(stored, produced);
            if (line < 0)
            {
                Debug.Log($"The report is the one in {file}.\n");
                return;
            }
            Assert.Fail(Difference(file, stored, produced, line, context));
        }

        /// <summary>
        /// What is written to a baseline: the circumstances, the
        /// <see cref="marker"/>, then the report.
        /// </summary>
        /// <param name="context">The circumstances the report was produced under.</param>
        /// <param name="report">What is held against the baseline.</param>
        /// <returns>The content of the baseline.</returns>
        private static string Content(string context, string report)
        {
            StringBuilder result = new();
            result.Append(context);
            result.AppendLine(marker);
            result.Append(report);
            return result.ToString();
        }

        /// <summary>
        /// The part of <paramref name="content"/> that is compared, that is,
        /// what follows the <see cref="marker"/>.
        /// </summary>
        /// <param name="content">The content of a baseline.</param>
        /// <returns>The report therein.</returns>
        private static string Payload(string content)
        {
            int at = content.IndexOf(marker, StringComparison.Ordinal);
            return at < 0 ? content : content.Substring(at + marker.Length);
        }

        /// <summary>
        /// The part of <paramref name="content"/> that is not compared, that
        /// is, what precedes the <see cref="marker"/>.
        /// </summary>
        /// <param name="content">The content of a baseline.</param>
        /// <returns>The circumstances recorded therein.</returns>
        private static string Head(string content)
        {
            int at = content.IndexOf(marker, StringComparison.Ordinal);
            return at < 0 ? string.Empty : content.Substring(0, at);
        }

        /// <summary>
        /// The commit at the tip of each branch as the baseline in
        /// <paramref name="content"/> records it.
        /// </summary>
        /// <param name="content">The content of a baseline.</param>
        /// <returns>The tips, keyed by the name of the branch.</returns>
        private static IDictionary<string, string> Tips(string content)
        {
            Dictionary<string, string> result = new();
            foreach (string line in Lines(Head(content)))
            {
                string[] parts = line.Split(' ');
                if (parts.Length == 3 && parts[0] == "tip")
                {
                    result[parts[1]] = parts[2];
                }
            }
            return result;
        }

        /// <summary>
        /// <paramref name="text"/> split into lines, whichever line endings
        /// it uses, with no empty line at its end.
        /// </summary>
        /// <param name="text">The text to be split.</param>
        /// <returns>The lines of the text.</returns>
        private static string[] Lines(string text)
        {
            return text.Replace("\r\n", "\n").Trim('\n').Split('\n');
        }

        /// <summary>
        /// The first index at which <paramref name="stored"/> and
        /// <paramref name="produced"/> differ, or -1 where they do not.
        /// </summary>
        /// <param name="stored">The lines of the baseline.</param>
        /// <param name="produced">The lines of the report just produced.</param>
        /// <returns>The index of the first difference, or -1.</returns>
        private static int FirstDifference(string[] stored, string[] produced)
        {
            for (int i = 0; i < Math.Min(stored.Length, produced.Length); i++)
            {
                if (stored[i] != produced[i])
                {
                    return i;
                }
            }
            return stored.Length == produced.Length ? -1 : Math.Min(stored.Length,
                                                                    produced.Length);
        }

        /// <summary>
        /// The account of how the report just produced differs from its
        /// baseline, and of whether the repository has meanwhile moved on.
        /// </summary>
        /// <param name="file">The baseline the report is held against.</param>
        /// <param name="stored">The lines of the baseline.</param>
        /// <param name="produced">The lines of the report just produced.</param>
        /// <param name="line">The index of the first differing line.</param>
        /// <param name="context">The circumstances the report was produced under.</param>
        /// <returns>The account.</returns>
        private static string Difference(string file, string[] stored, string[] produced,
                                         int line, string context)
        {
            StringBuilder result = new();
            result.AppendLine($"The report differs from the baseline in {file}.");
            result.AppendLine($"It has {produced.Length} lines where the baseline has "
                              + $"{stored.Length}; the first of them to differ is line "
                              + $"{line + 1}:");
            result.AppendLine($"  baseline: {At(stored, line)}");
            result.AppendLine($"  now:      {At(produced, line)}");

            IDictionary<string, string> before = Tips(File.ReadAllText(file));
            IDictionary<string, string> now = Tips(context + marker);
            ICollection<string> moved
                = now.Where(tip => !before.TryGetValue(tip.Key, out string sha)
                                   || sha != tip.Value)
                     .Select(tip => tip.Key).ToList();
            if (moved.Count == 0)
            {
                result.AppendLine("No branch has moved since, so the difference is not one "
                                  + "of history: the code deriving the report has changed "
                                  + "its answer.");
            }
            else
            {
                result.AppendLine($"{moved.Count} of {now.Count} branches have moved "
                                  + "since the baseline was written, which may account for "
                                  + "the difference: "
                                  + string.Join(", ", moved.Take(5))
                                  + (moved.Count > 5 ? ", ..." : string.Empty));
            }
            result.AppendLine($"Delete {file} to accept the report of this run instead.");
            return result.ToString();
        }

        /// <summary>
        /// The line of <paramref name="lines"/> at <paramref name="index"/>,
        /// or a note that there is none.
        /// </summary>
        /// <param name="lines">The lines to index into.</param>
        /// <param name="index">The index of the line asked for.</param>
        /// <returns>The line or a note.</returns>
        private static string At(string[] lines, int index)
        {
            return index < lines.Length ? lines[index] : "(no such line)";
        }

        /// <summary>
        /// <paramref name="testCase"/> with everything a filename may not
        /// contain replaced by an underscore.
        /// </summary>
        /// <param name="testCase">The name of a test case.</param>
        /// <returns>A name fit for a file.</returns>
        private static string FilenameOf(string testCase)
        {
            return string.Join("_", testCase.Split(Path.GetInvalidFileNameChars()));
        }
    }
}
