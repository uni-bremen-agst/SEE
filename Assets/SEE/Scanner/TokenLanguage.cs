using System.Collections.Generic;

namespace SEE.Scanner
{
    /// <summary>
    /// A programming language a <see cref="SEEToken.TokenType"/> is in.
    /// </summary>
    public abstract class TokenLanguage
    {
        /// <summary>
        /// Default number of spaces a tab is equivalent to.
        /// </summary>
        protected const int defaultTabWidth = 4;

        /// <summary>
        /// Language-independent symbolic name for the end of file token.
        /// </summary>
        protected const string eof = "EOF";

        /// <summary>
        /// The name of the language.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Number of spaces equivalent to a tab in this language.
        /// If not specified, this will be <see cref="defaultTabWidth"/>.
        /// </summary>
        public int TabWidth { get; }

        /// <summary>
        /// File extensions which apply for the given language.
        /// May not intersect any other languages file extensions.
        /// </summary>
        public ISet<string> FileExtensions { get; }

        /// <summary>
        /// A list of all token languages there are.
        /// </summary>
        public static readonly ISet<TokenLanguage> AllTokenLanguages = new HashSet<TokenLanguage>();

        protected TokenLanguage(string name, ISet<string> fileExtensions, int tabWidth = defaultTabWidth)
        {
            Name = name;
            TabWidth = tabWidth;
            FileExtensions = fileExtensions;
            AllTokenLanguages.Add(this);
        }
    }
}
