using DiffMatchPatch;
using System;
using System.Collections.Generic;
using System.Text;

namespace SEE.Utils
{
    /// <summary>
    /// 
    /// </summary>
    public static class TextualDiff
    {

        public static string[] Diff(string sourcePath, int sourceStartLine, int sourceEndLine,
                                    string targetPath, int targetStartLine, int targetEndLine)
        {
            diff_match_patch diff = new();
            string sourceLines = FileIO.Read(sourcePath, sourceStartLine, sourceEndLine);
            string targetLines = FileIO.Read(targetPath, targetStartLine, targetEndLine);
            return Diff2RichText(diff.diff_main(sourceLines, targetLines));
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
                        // red and stroke through
                        result.Append("<color=\"red\"><s>").Append(aDiff.text).Append("</s></color>");
                        break;
                    case Operation.DELETE:
                        // green and underlined
                        result.Append("<color=\"green\"><u>").Append(aDiff.text).Append("</u></color>");
                        break;
                    case Operation.EQUAL:
                        result.Append(aDiff.text);
                        break;
                }
            }
            return result.ToString().Split(new string[] { "\r\n", "\r", "\n" },
                                           StringSplitOptions.None);
        }
    }
}