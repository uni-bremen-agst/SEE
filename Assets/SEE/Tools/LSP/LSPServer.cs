using System.Collections.Generic;
using System.Linq;
using SEE.Utils;

namespace SEE.Tools.LSP
{
    /// <summary>
    /// Represents a language server.
    ///
    /// This record contains all information necessary to start a language server.
    /// </summary>
    public record LSPServer
    {
        /// <summary>
        /// The name of the language server.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The languages supported by this language server.
        /// </summary>
        /// <remarks>
        /// Note that the order matters: Specifically, earlier languages take precedence over later ones
        /// (e.g., when merging language ID mappings).
        /// </remarks>
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
        /// The options to be passed to the language server during initialization.
        /// </summary>
        public IDictionary<string, object> InitOptions { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">The name of the language server.</param>
        /// <param name="languages">The languages supported by this language server.</param>
        /// <param name="serverExecutable">The name of the executable of the language server.</param>
        /// <param name="parameters">The parameters with which the <paramref name="serverExecutable"/> should be invoked.</param>
        /// <param name="initOptions">The options to be passed to the language server during initialization.</param>
        private LSPServer(string name, IList<LSPLanguage> languages, string serverExecutable,
                          string parameters = "", IDictionary<string, object> initOptions = null)
        {
            Name = name;
            Languages = languages;
            ServerExecutable = serverExecutable;
            Parameters = parameters;
            InitOptions = initOptions;
            All.Add(this);
        }

        /// <summary>
        /// Returns the language ID for the given file <paramref name="extension"/>.
        /// </summary>
        /// <param name="extension">The file extension for which the language ID shall be returned.</param>
        /// <returns>The language ID for the given file <paramref name="extension"/>.</returns>
        public string LanguageIdFor(string extension)
        {
            LSPLanguage language = Languages.FirstOrDefault(language => language.Extensions.Contains(extension));
            return language?.LanguageIds.GetValueOrDefault(extension, language.LanguageIds.GetValueOrDefault(string.Empty));
        }

        public override string ToString()
        {
            return Name;
        }

        public static readonly IList<LSPServer> All = new List<LSPServer>();

        public static readonly LSPServer Clangd = new("clangd",
                                                      "https://clangd.llvm.org/",
                                                      new List<LSPLanguage> { LSPLanguage.C, LSPLanguage.CPP },
                                                      "clangd", "--background-index");

        public static readonly LSPServer Omnisharp = new("Omnisharp",
                                                         new List<LSPLanguage> { LSPLanguage.CSharp },
                                                         "omnisharp", "-z DotNet:enablePackageRestore=false -e utf-8 -lsp",
                                                         initOptions: new Dictionary<string, object>
                                                         {
                                                             {
                                                                 "RoslynExtensionOptions",
                                                                 new Dictionary<string, object>
                                                                 {
                                                                     { "enableDecompilationSupport", false },
                                                                     { "enableImportCompletion", false },
                                                                 }
                                                             }
                                                         });

        public static readonly LSPServer DartAnalysisServer = new("Dart analysis server",
                                                                  "https://github.com/dart-lang/sdk/blob/master/pkg/analysis_server/tool/lsp_spec/README.md",
                                                                  new List<LSPLanguage> { LSPLanguage.Dart },
                                                                  "dart", "language-server");

        public static readonly LSPServer Pyright = new("Pyright",
                                                       "https://github.com/microsoft/pyright",
                                                       new List<LSPLanguage> { LSPLanguage.Python },
                                                       "pyright-langserver",
                                                       "--stdio");

        public static readonly LSPServer RustAnalyzer = new("Rust Analyzer",
                                                            "https://github.com/rust-lang/rust-analyzer",
                                                            new List<LSPLanguage> { LSPLanguage.Rust },
                                                            "rust-analyzer",
                                                            initOptions: new Dictionary<string, object>
                                                            {
                                                                {
                                                                    "references", new Dictionary<string, object>
                                                                    {
                                                                        { "excludeImports", true }
                                                                    }
                                                                }
                                                            });

        /// <summary>
        /// Returns the language server with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the language server to be returned.</param>
        /// <returns>The language server with the given <paramref name="name"/>.</returns>
        public static LSPServer GetByName(string name)
        {
            return All.FirstOrDefault(server => server.Name == name);
        }
    }
}
