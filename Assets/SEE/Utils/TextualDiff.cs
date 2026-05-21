using DiffMatchPatch;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SEE.Utils
{
    /// <summary>
    /// Determines differences in two texts.
    /// </summary>
    public static class TextualDiff
    {
        /// <summary>
        /// Returns the unified difference of two text regions in two files. A text region
        /// is defined by the path to a file and a starting and ending line within this
        /// file. The unified diff contains Rich Text markup that indicates the additions
        /// and deletions between the two regions. An addition is text that is contained
        /// in the new region only and a deletion is text that is contained only in the
        /// old region.
        ///
        /// The Rich Text markup of a deletion renders the deleted text in red
        /// and struck through. The markup of an additions renders the added text
        /// in green and underlined.
        ///
        /// Each entry in the result is a line of text of the new region.
        /// </summary>
        /// <param name="oldPath">Path to the file containing the old region.</param>
        /// <param name="oldStartLine">Starting line of the old region.</param>
        /// <param name="oldEndLine">Ending line of the old region.</param>
        /// <param name="newPath">Path to the file containing the new region.</param>
        /// <param name="newStartLine">Starting line of the new region.</param>
        /// <param name="newEndLine">Ending line of the new region.</param>
        /// <returns>Unified diff in Rich Text markup.</returns>
        public static string[] Diff(string oldPath, int oldStartLine, int oldEndLine,
                                    string newPath, int newStartLine, int newEndLine)
        {
            diff_match_patch diff = new();
            string oldRegion = FileIO.Read(oldPath, oldStartLine, oldEndLine);
            string newRegion = FileIO.Read(newPath, newStartLine, newEndLine);
            return Diff2RichText(diff.diff_main(oldRegion, newRegion));
        }
        /// <summary>
        /// Returns the differences of <paramref name="current"/> relative to
        /// <paramref name="old"/>.
        /// The diff contains Rich Text markup that indicates the additions
        /// and deletions between the two inputs. An addition is text that is contained
        /// in <paramref name="current"/> but not in <paramref name="old"/> and a
        /// deletion is text that is contained only in <paramref name="old"/>.
        ///
        /// The Rich Text markup of a deletion renders the deleted text in red
        /// and struck through. The markup of an addition renders the added text
        /// in green and underlined.
        ///
        /// Each entry in the result is a line of text of <paramref name="current"/>.
        /// </summary>
        /// <param name="old">The old region.</param>
        /// <param name="current">The new region.</param>
        /// <returns>Diff in Rich Text markup.</returns>
        public static string[] Diff(string old, string current)
        {
            diff_match_patch diff = new();
            List<Diff> diffs = diff.diff_main(old, current);
            diff.diff_cleanupSemantic(diffs);
            return Diff2RichText(diffs);
        }

        /// <summary>
        /// Converts given list of <paramref name="diffs"/> into a Rich Text markup
        /// for TextMesh Pro highlighting the inserts and deletions.
        /// The result is split into lines (using the typical newline separators
        /// used on Linux, MacOS, or Windows).
        /// </summary>
        /// <param name="diffs">List of Diff objects.</param>
        /// <returns>Representation of diff in Rich Text markup.</returns>
        private static string[] Diff2RichText(IList<Diff> diffs)
        {
            StringBuilder result = new();
            foreach (Diff aDiff in diffs)
            {
                switch (aDiff.operation)
                {
                    case Operation.INSERT:
                        // green and underlined
                        result.Append("<color=\"green\"><u><noparse>").Append(ReplaceNewlines(aDiff.text)).Append("</noparse></u></color>");
                        break;
                    case Operation.DELETE:
                        // red and struck through
                        result.Append("<color=\"red\"><s><noparse>").Append(ReplaceNewlines(aDiff.text)).Append("</noparse></s></color>");
                        break;
                    case Operation.EQUAL:
                        result.Append("<noparse>").Append(ReplaceNewlines(aDiff.text)).Append("</noparse>");
                        break;
                    default:
                        throw new NotImplementedException($"Case {aDiff.operation} not supported.");
                }
            }
            return result.ToString().Split(new string[] { "\r\n", "\r", "\n" },
                                           StringSplitOptions.None);

            // We must close an open <noparse> for each newline and open it again after
            // the newline.
            static string ReplaceNewlines(string value)
            {
                return Regex.Replace(value, @"\r\n?|\n", "</noparse>\n<noparse>");
            }
        }
    }
}