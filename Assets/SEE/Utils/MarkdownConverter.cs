using System.Linq;
using Markdig;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using UnityEngine;

namespace SEE.Utils
{
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

            return $"<noparse>{MarkupTextToRichText(markdown)}</noparse>";
        }

        /// <summary>
        /// Converts the given markdown-formatted <paramref name="markdownText"/> to TextMeshPro-compatible rich text.
        /// </summary>
        /// <param name="markdownText">The markdown-formatted text to convert.</param>
        /// <returns>The converted rich text.</returns>
        public static string MarkupTextToRichText(string markdownText)
        {
            // TODO (#728): Parse markdown to TextMeshPro rich text (custom MarkDig parser).
            return Markdown.ToPlainText(markdownText);
        }
    }
}
