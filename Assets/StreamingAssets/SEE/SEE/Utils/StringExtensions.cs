using System.Text;

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
            StringBuilder builder = new StringBuilder();
            foreach (string inputLine in input.Split('\n'))
            {
                string line = inputLine;
                while (line.Length >= wrapAt)
                {
                    int lastSpace = line.Substring(0, wrapAt).LastIndexOf(' ');
                    if (lastSpace == -1)
                    {
                        lastSpace = wrapAt;
                    }
                    builder.Append(line.Substring(0, lastSpace) + '\n');
                    line = line.Substring(lastSpace).TrimStart(' ');
                }

                builder.Append(line + '\n');
            }

            return builder.Remove(builder.Length - 1, 1).ToString().TrimEnd('\n'); // remove excess newlines
        }
    }
}