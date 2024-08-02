using Markdig.Syntax.Inlines;

namespace SEE.Utils.Markdown
{
    /// <summary>
    /// Partial class that contains renderers for inline Markdown elements.
    /// </summary>
    public partial class RichTagsMarkdownRenderer
    {
        /// <summary>
        /// Renders an inline code span.
        /// </summary>
        private class CodeInlineRenderer : RichTagsObjectRenderer<CodeInline>
        {
            protected override void Write(RichTagsMarkdownRenderer renderer, CodeInline obj)
            {
                renderer.Write("<font=\"Hack-Regular SDF\">");
                renderer.WriteEscaped(obj.ContentSpan);
                renderer.Write("</font>");
            }
        }

        /// <summary>
        /// Renders a delimiter inline.
        /// </summary>
        private class DelimiterInlineRenderer : RichTagsObjectRenderer<DelimiterInline>
        {
            protected override void Write(RichTagsMarkdownRenderer renderer, DelimiterInline obj)
            {
                renderer.WriteEscaped(obj.ToLiteral());
                renderer.WriteChildren(obj);
            }
        }

        /// <summary>
        /// Renders an emphasized span of text.
        /// </summary>
        private class EmphasisInlineRenderer : RichTagsObjectRenderer<EmphasisInline>
        {
            protected override void Write(RichTagsMarkdownRenderer renderer, EmphasisInline obj)
            {
                if (obj.DelimiterCount == 1)
                {
                    renderer.Write("<i>");
                    renderer.WriteChildren(obj);
                    renderer.Write("</i>");
                }
                else
                {
                    renderer.Write("<b>");
                    renderer.WriteChildren(obj);
                    renderer.Write("</b>");
                }
            }
        }

        /// <summary>
        /// Renders a line break.
        /// </summary>
        private class LineBreakInlineRenderer : RichTagsObjectRenderer<LineBreakInline>
        {
            protected override void Write(RichTagsMarkdownRenderer renderer, LineBreakInline obj)
            {
                renderer.EnsureLine();
            }
        }

        /// <summary>
        /// Renders a link.
        /// </summary>
        private class LinkInlineRenderer : RichTagsObjectRenderer<LinkInline>
        {
            protected override void Write(RichTagsMarkdownRenderer renderer, LinkInline obj)
            {
                // Links are hard to emulate with TextMeshPro, so we'll just use the URL as the text
                // in parentheses.
                string url = obj.GetDynamicUrl?.Invoke() ?? obj.Url;
                renderer.WriteChildren(obj);
                renderer.Write($" ({url})");
            }
        }

        /// <summary>
        /// Renders a literal inline.
        /// </summary>
        private class LiteralInlineRenderer : RichTagsObjectRenderer<LiteralInline>
        {
            protected override void Write(RichTagsMarkdownRenderer renderer, LiteralInline obj)
            {
                renderer.WriteEscaped(obj.Content.AsSpan());
            }
        }
    }
}
