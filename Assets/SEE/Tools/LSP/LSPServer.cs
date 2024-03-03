using System.Collections.Generic;
using System.Linq;

namespace SEE.Tools.LSP
{
    /// <summary>
    /// Represents a language server.
    ///
    /// This record contains all information necessary to start a language server.
    /// </summary>
    /// <seealso cref="LSPServers"/>
    public record LSPServer
    {
        /// <summary>
        /// The name of the language server.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The languages supported by this language server.
        /// </summary>
        public IList<LSPLanguage> Languages { get; }

        /// <summary>
        /// The name of the executable of the language server.
        /// </summary>
        public string ServerExecutable { get; }

        /// <summary>
        /// The parameters with which the <see cref="ServerExecutable"/> should be invoked.
        /// </summary>
        public string Parameters { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The name of the language server.</param>
        /// <param name="languages">The languages supported by this language server.</param>
        /// <param name="serverExecutable">The name of the executable of the language server.</param>
        /// <param name="parameters">The parameters with which the <paramref name="serverExecutable"/> should be invoked.</param>
        private LSPServer(string name, IList<LSPLanguage> languages, string serverExecutable, string parameters = "")
        {
            Name = name;
            Languages = languages;
            ServerExecutable = serverExecutable;
            Parameters = parameters;
            All.Add(this);
        }

        public override string ToString()
        {
            return Name;
        }

        public static readonly IList<LSPServer> All = new List<LSPServer>();

        public static readonly LSPServer RustAnalyzer = new("Rust Analyzer",
                                                            new List<LSPLanguage> { LSPLanguage.Rust },
                                                            "rust-analyzer");

        public static readonly LSPServer Pyright = new("Pyright",
                                                       new List<LSPLanguage> { LSPLanguage.Python },
                                                       "pyright-langserver", "--stdio");

        /// <summary>
        /// Returns the language server with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the language server to be returned.</param>
        /// <returns>The language server with the given <paramref name="name"/>.</returns>
        public static LSPServer GetByName(string name)
        {
            return All.First(server => server.Name == name);
        }
    }
}
