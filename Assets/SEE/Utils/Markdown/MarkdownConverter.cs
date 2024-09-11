using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoreLinq.Extensions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using UnityEngine;

namespace SEE.Utils.Markdown
{
    /// <summary>
    /// Utility class for converting markdown text to TextMeshPro-compatible rich text.
    /// </summary>
    public static class MarkdownConverter
    {
        /// <summary>
        /// Converts the given <paramref name="content"/> to TextMeshPro-compatible rich text.
        /// </summary>
        /// <param name="content">The content to convert.</param>
        /// <returns>The converted rich text.</returns>
        public static string ToRichText(this MarkedStringsOrMarkupContent content)
        {
            string markdown;
            if (content.HasMarkupContent)
            {
                MarkupContent markup = content.MarkupContent!;
                switch (markup.Kind)
                {
                    case MarkupKind.PlainText: return $"<noparse>{markup.Value}</noparse>";
                    case MarkupKind.Markdown:
                        markdown = markup.Value;
                        break;
                    default:
                        Debug.LogError($"Unsupported markup kind: {markup.Kind}");
                        return string.Empty;
                }
            }
            else
            {
                // This is technically deprecated, but we still need to support it,
                // since some language servers still use it.
                Container<MarkedString> strings = content.MarkedStrings!;
                markdown = string.Join("\n", strings.Select(x =>
                {
                    if (x.Language != null)
                    {
                        return $"```{x.Language}\n{x.Value}\n```";
                    }
                    else
                    {
                        return x.Value;
                    }
                }));
            }

            string richText = MarkupTextToRichText(markdown);
            // We concatenate empty successive lines, which may sometimes appear in the converted rich text.
            // To check if a line is empty, we need to get rid of its tags first.
            return string.Join('\n', richText.Split('\n')
                                             // We start a new segment whenever the line does not only consist of
                                             // white space.
                                             .Segment(x => !string.IsNullOrWhiteSpace(x.WithoutRichTextTags()))
                                             // Then, we join the segments with a single line break.
                                             // This way, we make sure not to accidentally remove rich text tags.
                                             .Select(HandleSegment));

            string HandleSegment(IEnumerable<string> segment)
            {
                IList<string> lines = segment.ToList();
                if (lines.Count == 1)
                {
                    return lines[0];
                }
                else
                {
                    // First line should be separated by a line break so that at least one line break is present.
                    return lines[0] + '\n' + string.Join(string.Empty, lines.Skip(1));
                }
            }
        }

        /// <summary>
        /// Converts the given markdown-formatted <paramref name="markdownText"/> to TextMeshPro-compatible rich text.
        /// </summary>
        /// <param name="markdownText">The markdown-formatted text to convert.</param>
        /// <returns>The converted rich text.</returns>
        public static string MarkupTextToRichText(string markdownText)
        {
            StringWriter writer = new();
            Markdig.Markdown.Convert(markdownText, new RichTagsMarkdownRenderer(writer));
            return writer.ToString();
        }
    }
}
