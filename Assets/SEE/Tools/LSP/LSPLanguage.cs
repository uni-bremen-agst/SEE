using System.Collections.Generic;

namespace SEE.Tools.LSP
{
    /// <summary>
    /// A programming language supported by a language server.
    /// </summary>
    /// <seealso cref="LSPServer"/>
    /// <seealso cref="LSPLanguages"/>
    public record LSPLanguage
    {
        /// <summary>
        /// The name of the language.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The file extensions associated with this language.
        /// </summary>
        public ISet<string> Extensions { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The name of the language.</param>
        /// <param name="extensions">The file extensions associated with this language.</param>
        public LSPLanguage(string name, ISet<string> extensions)
        {
            Name = name;
            Extensions = extensions;
            All.Add(this);
        }

        public static readonly IList<LSPLanguage> All = new List<LSPLanguage>();
        public static readonly LSPLanguage Rust = new("Rust", new HashSet<string> { "rs" });
        public static readonly LSPLanguage Python = new("Python", new HashSet<string> { "py" });
    }
}
