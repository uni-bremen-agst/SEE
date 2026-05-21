using System;
using System.IO;
using JetBrains.Annotations;
using Markdig.Helpers;
using Markdig.Renderers;
using Markdig.Syntax;

namespace SEE.Utils.Markdown
{
    /// <summary>
    /// A custom Markdig renderer that renders Markdown input to TextMeshPro-compatible rich text.
    /// </summary>
    public partial class RichTagsMarkdownRenderer : TextRendererBase<RichTagsMarkdownRenderer>
    {
        public RichTagsMarkdownRenderer([NotNull] TextWriter writer) : base(writer)
        {
            // Default block renderers
            ObjectRenderers.Add(new CodeBlockRenderer());
            ObjectRenderers.Add(new ListRenderer());
            ObjectRenderers.Add(new HeadingRenderer());
            ObjectRenderers.Add(new ParagraphRenderer());
            ObjectRenderers.Add(new QuoteRenderer());

            // Default inline renderers
            ObjectRenderers.Add(new CodeInlineRenderer());
            ObjectRenderers.Add(new DelimiterInlineRenderer());
            ObjectRenderers.Add(new EmphasisInlineRenderer());
            ObjectRenderers.Add(new LineBreakInlineRenderer());
            ObjectRenderers.Add(new LinkInlineRenderer());
            ObjectRenderers.Add(new LiteralInlineRenderer());
        }

        /// <summary>
        /// Writes the given <paramref name="group"/> of string lines.
        /// </summary>
        /// <param name="group">The group of string lines to write.</param>
        private void WriteStringLines(StringLineGroup group)
        {
            StringLine[] lines = group.Lines;
            // IDEs wrongly suggest that `lines` cannot be null, but it can be.
            // ReSharper disable ConditionIsAlwaysTrueOrFalse
            // ReSharper disable HeuristicUnreachableCode
            if (lines is null)
            {
                return;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Slice.Text != null && lines[i].Slice.IsEmptyOrWhitespace())
                {
                    // If this line is empty, we shouldn't write anything.
                    continue;
                }
                if (i > 0)
                {
                    WriteLine();
                }
                WriteEscaped(lines[i].Slice.AsSpan());
            }
        }

        /// <summary>
        /// Writes the given <paramref name="span"/> of characters, surrounded by a noparse-tag.
        /// </summary>
        /// <param name="span">The span of characters to write.</param>
        private void WriteEscaped(ReadOnlySpan<char> span)
        {
            Write("<noparse>");
            Write(span);
            Write("</noparse>");
        }

        /// <summary>
        /// An object renderer that renders <typeparamref name="TObject"/>-typed instances of Markdown objects
        /// as TextMeshPro-compatible rich text.
        /// </summary>
        /// <typeparam name="TObject">The type of Markdown object to render.</typeparam>
        private abstract class RichTagsObjectRenderer<TObject> : MarkdownObjectRenderer<RichTagsMarkdownRenderer, TObject> where TObject : MarkdownObject
        {
        }
    }
}
