using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Scanner;

namespace SEE.Tools.LSP
{
    /// <summary>
    /// A programming language supported by a language server.
    /// </summary>
    /// <seealso cref="LSPServer"/>
    public class LSPLanguage: TokenLanguage
    {
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
        private LSPLanguage(string name, ISet<string> extensions, IDictionary<string, string> languageIds = null): base(name, extensions)
        {
            if (name.Contains('/'))
            {
                throw new ArgumentException("Language name must not contain slashes!");
            }
            LanguageIds = languageIds ?? new Dictionary<string, string>();
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
            return AllLspLanguages.First(language => language.Name == name);
        }

        public override string ToString()
        {
            return Name;
        }

        public static readonly LSPLanguage C = new("C", new HashSet<string> { "c", "h" }, "c");
        public static readonly LSPLanguage CPP = new("C++", new HashSet<string>
        {
            "c", "C", "cc", "cpp", "cxx", "c++", "h", "H", "hh", "hpp", "hxx", "h++", "cppm", "ixx"
        }, "cpp");
        public static readonly LSPLanguage CSharp = new("C#", new HashSet<string> { "cs", "vb" }, "csharp");
        public static readonly LSPLanguage Dart = new("Dart", new HashSet<string> { "dart" }, "dart");
        public static readonly LSPLanguage Go = new("Go", new HashSet<string> { "go" }, "go");
        public static readonly LSPLanguage Haskell = new("Haskell", new HashSet<string> { "hs", "lhs" }, "haskell");
        public static readonly LSPLanguage Java = new("Java", new HashSet<string> { "java" }, "java");
        public static readonly LSPLanguage JavaScript = new("JavaScript", new HashSet<string>
        {
            "js", "cjs", "mjs"
        }, "javascript");
        public static readonly LSPLanguage JSON = new("JSON", new HashSet<string> { "json" }, "json");
        public static readonly LSPLanguage Kotlin = new("Kotlin", new HashSet<string> { "kt", "kts" }, "kotlin");
        public static readonly LSPLanguage LaTeX = new("LaTeX", new HashSet<string> { "tex", "latex" }, "latex");
        public static readonly LSPLanguage Lua = new("Lua", new HashSet<string> { "lua" }, "lua");
        public static readonly LSPLanguage Markdown = new("Markdown", new HashSet<string> { "md" }, "markdown");
        public static readonly LSPLanguage MATLAB = new("MATLAB", new HashSet<string>
        {
            "m", "p", "mex*", "mat", "fig", "mlx", "mlapp", "mltbx"
        }, "matlab");
        public static readonly LSPLanguage PHP = new("PHP", new HashSet<string>
        {
            "php", "phar", "phtml", "pht", "phps"
        }, "php");
        public static readonly LSPLanguage Python = new("Python", new HashSet<string> { "py" }, "python");
        public static readonly LSPLanguage Ruby = new("Ruby", new HashSet<string> { "rb", "ru" }, "ruby");
        public static readonly LSPLanguage Rust = new("Rust", new HashSet<string> { "rs" }, "rust");
        public static readonly LSPLanguage TypeScript = new("TypeScript", new HashSet<string>
        {
            "ts", "tsx", "mts", "cts"
        }, "typescript");
        public static readonly LSPLanguage XML = new("XML", new HashSet<string> { "xml", "gxl" }, "xml");
        public static readonly LSPLanguage Zig = new("Zig", new HashSet<string> { "zig" }, "zig");

        /// <summary>
        /// A list of all supported LSP languages.
        /// </summary>
        public static readonly IList<LSPLanguage> AllLspLanguages = AllTokenLanguages.OfType<LSPLanguage>().ToList();
    }
}
