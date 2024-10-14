using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SEE.Utils
{
    /// <summary>
    /// This class provides string extension methods.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Wraps this string at least every <paramref name="wrapAt"/> characters.
        /// Will only wrap around a space.
        /// </summary>
        /// <param name="input">The string to wrap.</param>
        /// <param name="wrapAt">Maximum number of characters after which a line shall be wrapped.</param>
        /// <returns>A multiline string in which each line is at most <paramref name="wrapAt"/> characters long,
        /// wrapping word-wise.</returns>
        public static string WrapLines(this string input, int wrapAt)
        {
            StringBuilder builder = new();
            foreach (string inputLine in input.Split('\n'))
            {
                string line = inputLine;
                while (line.Length >= wrapAt)
                {
                    int lastSpace = line[..wrapAt].LastIndexOf(' ');
                    if (lastSpace == -1)
                    {
                        lastSpace = wrapAt;
                    }
                    builder.Append(line[..lastSpace] + '\n');
                    line = line[lastSpace..].TrimStart(' ');
                }

                builder.Append(line + '\n');
            }

            return builder.Remove(builder.Length - 1, 1).ToString().TrimEnd('\n'); // remove excess newlines
        }

        /// <summary>
        /// All supported rich text tags.
        /// </summary>
        private static readonly string[] richTextTags =
        {
            "align", "allcaps", "alpha", "b", "color", "cspace", "font", "font-weight", "gradient", "i", "indent",
            "line-height", "line-indent", "link", "lowercase", "margin", "mark", "mspace", "nobr",
            "page", "pos", "rotate", "s", "size", "smallcaps", "space", "sprite", "strikethrough", "sub", "sup",
            "u", "uppercase", "voffset"
        };

        /// <summary>
        /// A regex that matches all rich text tags.
        /// </summary>
        private static readonly Regex richTextTagsRegex = new($"</?({string.Join('|', richTextTags)})[ =]?[^>]*?>",
                                                              RegexOptions.Compiled);

        /// <summary>
        /// Removes all rich text tags from this string.
        /// Note that content inside <noparse> tags will be ignored.
        /// </summary>
        /// <param name="input">The string to clean.</param>
        /// <returns>The string without any rich text tags.</returns>
        public static string WithoutRichTextTags(this string input)
        {
            StringBuilder builder = new();
            string[] segments = Regex.Split(input, "(</?noparse>)");
            int noparseCount = 0;
            foreach (string segment in segments)
            {
                if (segment == "<noparse>")
                {
                    noparseCount++;
                }
                else if (segment == "</noparse>")
                {
                    noparseCount--;
                }
                else if (noparseCount == 0)
                {
                    // We need to delete all tags in here.
                    builder.Append(richTextTagsRegex.Replace(segment, ""));
                }
                else
                {
                    builder.Append(segment);
                }
            }

            if (noparseCount > 0)
            {
                Debug.LogWarning("Unbalanced <noparse> tags in rich text. Original text:\n" + input);
            }

            return builder.ToString();
        }
    }
}
