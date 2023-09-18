using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace profiling2gxl
{
    internal class Helper
    {
        /// <summary>
        /// Transforms given snake_case to PascalCase.
        /// </summary>
        /// <param name="input">String in snake_case</param>
        /// <returns>The given string as PascalCase</returns>
        public static string SnakeToPascalCase(string input)
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            string[] words = input.Split('_');
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = textInfo.ToTitleCase(words[i]);
            }
            return string.Join("", words);
        }

        /// <summary>
        /// Transforms given PascalCase to snake_case.
        /// </summary>
        /// <param name="input">String in PascalCase</param>
        /// <returns>The given string as snake_case</returns>
        public static string PascalToSnakeCase(string input)
        {
            string[] words = Regex.Split(input, @"(?<!^)(?=[A-Z])");
            return string.Join("_", words);
        }

        /// <summary>
        /// Loads a XML using <see cref="XDocument.Load"/> from a <see cref="TextReader"/>.
        /// </summary>
        /// <param name="sr">
        /// A <see cref="TextReader"/> containing the raw XML to read into the newly
        /// created <see cref="XDocument"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="XDocument"/> containing the contents of the passed in
        /// <see cref="TextReader"/>.
        /// </returns>
        public static XDocument loadXML(StreamReader sr)
        {
            return XDocument.Load(sr);
        }
    }
}
