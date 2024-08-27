namespace SEE.Game.Drawable.ActionHelpers
{
    /// <summary>
    /// This class is responsible for removing the rich text tags of an input string.
    /// Inspired by Jayden Chapman (https://gitlab.com/-/snippets/2031682)
    /// </summary>
    public static class RichTextRemover
    {
        /// <summary>
        /// The non-dynamic tags, which cannot be assigned any properties.
        /// </summary>
        private static readonly string[] NonDynamic = new string[]
        {
            "b", "br",  "i", "u", "s", "strikethrough", "sup", "sub", "allcaps", "lowercase", "smallcaps", "uppercase"
        };

        /// <summary>
        /// The dynamic tags, wich can be assigned properties.
        /// </summary>
        private static readonly string[] Dynamic = new string[]
        {
            "alpha", "color", "align", "size", "cspace", "font", "font-weight", "gradient",
            "indent", "line-height", "line-indent", "link", "margin", "margin-left",
            "margin-right", "mark", "mspace", "noparse", "nobr", "page", "pos", "rotate", "space",
            "sprite index", "sprite name", "sprite", "style", "voffset", "width",
        };

        /// <summary>
        /// Removes the dynamic and non-dynamic Rich Text tags from an <see cref="input"/>.
        /// </summary>
        /// <param name="input">The given input.</param>
        /// <returns>The input without Rich Text tags.</returns>
        public static string RemoveRichText(string input)
        {
            foreach (string tag in Dynamic)
            {
                input = RemoveDynamicTag(input, tag.ToString());
            }
            foreach (string tag in NonDynamic)
            {
                input = RemoveNonDynamicTag(input, tag.ToString());
            }
            return input;
        }

        /// <summary>
        /// Removes the dynamic rich text tags from a text.
        /// </summary>
        /// <param name="input">The given input.</param>
        /// <param name="tag">The tag to be removed.</param>
        /// <returns>The input without the given tag.</returns>
        private static string RemoveDynamicTag(string input, string tag)
        {
            int index = input.IndexOf($"<{tag}=");
            if (index != -1)
            {
                int endIndex = input.Substring(index, input.Length - index).IndexOf('>');
                if (endIndex > 0)
                {
                    input = input.Remove(index, endIndex + 1);
                }
                return RemoveDynamicTag(input, tag);
            }
            else
            {
                return RemoveNonDynamicTag(input, tag, false);
            }
        }

        /// <summary>
        /// Removes the non-dynamic Rich Text tags.
        /// </summary>
        /// <param name="input">The given input.</param>
        /// <param name="tag">The tag to be removed.</param>
        /// <param name="isStart">Whether the start or end tag should be searched.</param>
        /// <returns>The input without the given tag.</returns>
        private static string RemoveNonDynamicTag(string input, string tag, bool isStart = true)
        {
            int index = input.IndexOf(isStart ? $"<{tag}>" : $"</{tag}>");
            if (index != -1)
            {
                input = input.Remove(index, 2 + tag.Length + (!isStart).GetHashCode());
                return RemoveNonDynamicTag(input, tag, isStart);
            }
            if (isStart)
            {
                input = RemoveNonDynamicTag(input, tag, false);
            }
            return input;
        }
    }
}
