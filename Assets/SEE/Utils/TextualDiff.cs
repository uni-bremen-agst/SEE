using DiffMatchPatch;
using System;
using System.Collections.Generic;
using System.Text;

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
        /// <param name="oldPath">path to the file containing the old region</param>
        /// <param name="oldStartLine">starting line of the old region</param>
        /// <param name="oldEndLine">ending line of the old region</param>
        /// <param name="newPath">path to the file containing the new region</param>
        /// <param name="newStartLine">starting line of the new region</param>
        /// <param name="newEndLine">ending line of the new region</param>
        /// <returns>unified diff in Rich Text markup</returns>
        public static string[] Diff(string oldPath, int oldStartLine, int oldEndLine,
                                    string newPath, int newStartLine, int newEndLine)
        {
            diff_match_patch diff = new();
            string oldRegion = FileIO.Read(oldPath, oldStartLine, oldEndLine);
            string newRegion = FileIO.Read(newPath, newStartLine, newEndLine);
            return Diff2RichText(diff.diff_main(oldRegion, newRegion));
        }

        /// <summary>
        /// Converts given list of <paramref name="diffs"/> into a Rich Text markup
        /// for TextMesh Pro highlighting the inserts and deletions.
        /// The result is split into lines (using the typical newline separators
        /// used on Linux, MacOS, or Windows).
        /// </summary>
        /// <param name="diffs">List of Diff objects</param>
        /// <returns>representation of diff in Rich Text markup</returns>
        private static string[] Diff2RichText(IList<Diff> diffs)
        {
            StringBuilder result = new();
            foreach (Diff aDiff in diffs)
            {
                switch (aDiff.operation)
                {
                    case Operation.INSERT:
                        // red and struck through
                        result.Append("<color=\"red\"><s><noparse>").Append(aDiff.text).Append("</noparse></s></color>");
                        break;
                    case Operation.DELETE:
                        // green and underlined
                        result.Append("<color=\"green\"><u><noparse>").Append(aDiff.text).Append("</noparse></u></color>");
                        break;
                    case Operation.EQUAL:
                        result.Append("<noparse>").Append(aDiff.text).Append("</noparse>");
                        break;
                    default:
                        throw new NotImplementedException($"Case {aDiff.operation} not supported.");
                }
            }
            return result.ToString().Split(new string[] { "\r\n", "\r", "\n" },
                                           StringSplitOptions.None);
        }
    }
}