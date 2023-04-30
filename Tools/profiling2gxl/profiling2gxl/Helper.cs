using System.Globalization;
using System.Text.RegularExpressions;

namespace profiling2gxl
{
    internal class Helper
    {
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

        public static string PascalToSnakeCase(string input)
        {
            string[] words = Regex.Split(input, @"(?<!^)(?=[A-Z])");
            return string.Join("_", words);
        }
    }
}
