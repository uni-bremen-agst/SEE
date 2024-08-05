using System;
using Markdig.Syntax;

namespace SEE.Utils.Markdown
{
    /// <summary>
    /// Partial class that contains renderers for block Markdown elements.
    /// </summary>
    public partial class RichTagsMarkdownRenderer
    {
        /// <summary>
        /// Renders a code block.
        /// </summary>
        private class CodeBlockRenderer : RichTagsObjectRenderer<CodeBlock>
        {
            protected override void Write(RichTagsMarkdownRenderer renderer, CodeBlock obj)
            {
                renderer.EnsureLine();

                renderer.Write("<font=\"Hack-Regular SDF\">");
                renderer.WriteStringLines(obj.Lines);
                renderer.Write("</font>");

                renderer.EnsureLine();
            }
        }

        /// <summary>
        /// Renders a heading.
        /// </summary>
        private class HeadingRenderer : RichTagsObjectRenderer<HeadingBlock>
        {
            protected override void Write(RichTagsMarkdownRenderer renderer, HeadingBlock obj)
            {
                renderer.EnsureLine();

                renderer.Write($"<style=H{Math.Min(obj.Level, 3)}>");
                renderer.WriteLeafInline(obj);
                renderer.Write("</style>");

                renderer.EnsureLine();
            }
        }

        /// <summary>
        /// Renders a quote block.
        /// </summary>
        private class QuoteRenderer : RichTagsObjectRenderer<QuoteBlock>
        {
            protected override void Write(RichTagsMarkdownRenderer renderer, QuoteBlock obj)
            {
                renderer.EnsureLine();

                renderer.Write("<style=Quote>");
                renderer.WriteChildren(obj);
                renderer.Write("</style>");

                renderer.EnsureLine();
            }
        }

        /// <summary>
        /// Renders a list block.
        /// </summary>
        private class ListRenderer : RichTagsObjectRenderer<ListBlock>
        {
            protected override void Write(RichTagsMarkdownRenderer renderer, ListBlock obj)
            {
                const char bullet = '•';
                renderer.EnsureLine();
                foreach (Block block in obj)
                {
                    renderer.EnsureLine();
                    renderer.Write(bullet);
                    renderer.Write(' ');
                    renderer.WriteChildren((ListItemBlock)block);
                    renderer.EnsureLine();
                }
                renderer.EnsureLine();
            }
        }

        /// <summary>
        /// Renders a paragraph block.
        /// </summary>
        private class ParagraphRenderer : RichTagsObjectRenderer<ParagraphBlock>
        {
            protected override void Write(RichTagsMarkdownRenderer renderer, ParagraphBlock obj)
            {
                renderer.WriteLeafInline(obj);
                renderer.EnsureLine();
            }
        }
    }
}
