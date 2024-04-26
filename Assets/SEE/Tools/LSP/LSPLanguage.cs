using System;
using System.Collections.Generic;
using System.Linq;

namespace SEE.Tools.LSP
{
    /// <summary>
    /// A programming language supported by a language server.
    /// </summary>
    /// <seealso cref="LSPServer"/>
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
        /// A mapping from file extensions to LSP language IDs.
        ///
        /// Note that the empty string key is used for the default language ID.
        /// </summary>
        public IDictionary<string, string> LanguageIds { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The name of the language.</param>
        /// <param name="extensions">The file extensions associated with this language.</param>
        /// <param name="languageIds">A mapping from file extensions to LSP language IDs.</param>
        private LSPLanguage(string name, ISet<string> extensions, IDictionary<string, string> languageIds = null)
        {
            if (name.Contains('/'))
            {
                throw new ArgumentException("Language name must not contain slashes!");
            }
            Name = name;
            Extensions = extensions;
            LanguageIds = languageIds ?? new Dictionary<string, string>();
            All.Add(this);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The name of the language.</param>
        /// <param name="extensions">The file extensions associated with this language.</param>
        /// <param name="languageId">The LSP language ID for this language.</param>
        private LSPLanguage(string name, ISet<string> extensions, string languageId) : this(name, extensions)
        {
            LanguageIds = new Dictionary<string, string> { { string.Empty, languageId } };
        }

        /// <summary>
        /// Returns the language with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the language to be returned.</param>
        /// <returns>The language with the given <paramref name="name"/>.</returns>
        public static LSPLanguage GetByName(string name)
        {
            return All.First(language => language.Name == name);
        }

        public override string ToString()
        {
            return Name;
        }

        public static readonly IList<LSPLanguage> All = new List<LSPLanguage>();
        public static readonly LSPLanguage C = new("C", new HashSet<string> { "c", "h" }, "c");
        public static readonly LSPLanguage CPP = new("C++", new HashSet<string>
        {
            "c", "C", "cc", "cpp", "cxx", "c++", "h", "H", "hh", "hpp", "hxx", "h++", "cppm", "ixx"
        }, "cpp");
        public static readonly LSPLanguage CSharp = new("C#", new HashSet<string> { "cs", "vb" }, "csharp");
        public static readonly LSPLanguage Dart = new("Dart", new HashSet<string> { "dart" }, "dart");
        public static readonly LSPLanguage Python = new("Python", new HashSet<string> { "py" }, "python");
        public static readonly LSPLanguage Rust = new("Rust", new HashSet<string> { "rs" }, "rust");
    }
}
