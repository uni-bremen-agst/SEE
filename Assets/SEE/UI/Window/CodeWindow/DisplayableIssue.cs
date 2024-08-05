using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using SEE.DataModel.DG;

namespace SEE.UI.Window.CodeWindow
{
    /// <summary>
    /// A code issue or diagnostic that can be displayed in code windows.
    /// </summary>
    public interface IDisplayableIssue
    {
        /// <summary>
        /// Returns the message of the issue as a string.
        /// This message may need to be retrieved asynchronously.
        /// </summary>
        /// <returns>The message of the issue.</returns>
        UniTask<string> ToDisplayStringAsync();

        /// <summary>
        /// The source of the issue, for example, "LSP" or "Axivion".
        /// </summary>
        string Source { get; }

        /// <summary>
        /// All occurrences of this issue in the code.
        /// </summary>
        IEnumerable<(string Path, Range Range)> Occurrences { get; }

        /// <summary>
        /// A list of rich tags that should be used to render the issue.
        /// These will include the tag separators. For example, an entry here might be <![CDATA[ <mark=green> ]]>.
        /// </summary>
        IList<string> RichTags { get; }

        /// <summary>
        /// The opening rich tags. Should be put in front of any occurrence of the issue.
        /// </summary>
        string OpeningRichTags => string.Join("", RichTags);

        /// <summary>
        /// The closing rich tags. Should be put after any occurrence of the issue.
        /// </summary>
        string ClosingRichTags => string.Join("", RichTags.Reverse().Select(ToClosingTag));

        /// <summary>
        /// Converts the given <paramref name="openingTag"/> to a closing TextMeshPro tag.
        /// </summary>
        /// <param name="openingTag">The opening tag to convert.</param>
        /// <returns>The closing tag that corresponds to the given <paramref name="openingTag"/>.</returns>
        private static string ToClosingTag(string openingTag) => new Regex(@"<([^\s=]*)[ =]?.*>").Replace(openingTag, "</$1>");

        /// <summary>
        /// Whether the rich tags contain any color tags.
        /// </summary>
        bool HasColorTags => RichTags.Any(t => t.StartsWith("<color"));

        /// <summary>
        /// Returns a string describing the issue that can be displayed in a code window.
        /// Hence, this may contain rich text tags.
        /// </summary>
        /// <returns>A string describing the issue that can be displayed in a code window.</returns>
        public async UniTask<string> ToCodeWindowStringAsync()
        {
            string message = await ToDisplayStringAsync();
            return $"{message}\n\n<color=#C0C0C0><size=70%>Source: {Source}</size></color>";
        }

        /// <summary>
        /// Returns the range of characters (the end character being exclusive) that contain an
        /// occurrence of this issue in the given line.
        /// If such an occurrence either does not exist, or is ambiguous, this method returns null.
        /// </summary>
        /// <param name="path">The path of the file that contains the line.</param>
        /// <param name="lineNumber">The line number of the line that contains the issue.</param>
        /// <param name="line">The content of the line that contains the issue.</param>
        /// <returns>The range of characters that contain an occurrence of this issue in the given line,
        /// or null if such an occurrence does not exist or is ambiguous.</returns>
        (int startCharacter, int endCharacter)? GetCharacterRangeForLine(string path, int lineNumber, string line)
        {
            return Occurrences
                   .Where(o => o.Range.HasCharacter)
                   .Where(o => o.Path == path && o.Range.Overlaps(lineNumber))
                   .Select<(string Path, Range Range), (int, int)?>(o =>
                   {
                       if (o.Range.StartLine == o.Range.EndLine)
                       {
                           // We are on the only line of this issue.
                           return (o.Range.StartCharacter!.Value, o.Range.EndCharacter!.Value);
                       }
                       else if (lineNumber == o.Range.StartLine)
                       {
                           // We are on the first line of this issue.
                           return (o.Range.StartCharacter!.Value, line.Length + 1);
                       }
                       else if (lineNumber == o.Range.EndLine)
                       {
                           // We are on the last line of this issue.
                           return (0, o.Range.EndCharacter!.Value);
                       }
                       else
                       {
                           // We are on a line in between the first and last line of this issue.
                            return (0, line.Length + 1);
                       }
                   })
                   .DefaultIfEmpty(null)
                   .First();
        }
    }
}
