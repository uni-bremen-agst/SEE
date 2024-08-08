using System.Collections.Generic;
using System.Linq;
using SEE.Utils;
using static SEE.Tools.LSP.LSPLanguage;

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
        /// A URL to the website of the language server.
        ///
        /// The website should contain instructions on how to install the language server.
        /// </summary>
        public string WebsiteURL { get; }

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
        /// <param name="websiteURL">A URL to the website of the language server.</param>
        /// <param name="languages">The languages supported by this language server.</param>
        /// <param name="serverExecutable">The name of the executable of the language server.</param>
        /// <param name="parameters">The parameters with which the <paramref name="serverExecutable"/> should be invoked.</param>
        /// <param name="initOptions">The options to be passed to the language server during initialization.</param>
        private LSPServer(string name, string websiteURL, IList<LSPLanguage> languages, string serverExecutable,
                          string parameters = "", IDictionary<string, object> initOptions = null)
        {
            Name = name;
            WebsiteURL = websiteURL;
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
            LSPLanguage language = Languages.FirstOrDefault(language => language.FileExtensions.Contains(extension));
            return language?.LanguageIds.GetValueOrDefault(extension, language.LanguageIds.GetValueOrDefault(string.Empty));
        }

        public override string ToString()
        {
            return Name;
        }

        public static readonly IList<LSPServer> All = new List<LSPServer>();

        // NOTE: All servers below have been tested first. Before adding a language server to this list,
        //       please make sure that it actually works in SEE, since we have some special requirements
        //       (e.g., we require a documentSymbol provider that returns hierarchic `DocumentSymbol` objects).

        public static readonly LSPServer Clangd = new("clangd",
                                                      "https://clangd.llvm.org/",
                                                      new List<LSPLanguage> { C, CPP },
                                                      "clangd", "--background-index");

        public static readonly LSPServer Gopls = new("gopls",
                                                     "https://github.com/golang/tools/tree/master/gopls",
                                                     new List<LSPLanguage> { Go },
                                                     "gopls");

        public static readonly LSPServer HaskellLanguageServer = new("Haskell Language Server",
                                                                     "https://haskell-language-server.readthedocs.io/en/latest/",
                                                                     new List<LSPLanguage> { Haskell },
                                                                     "haskell-language-server", "--lsp");

        public static readonly LSPServer EclipseJdtls = new("Eclipse JDT Language Server",
                                                            "https://github.com/eclipse-jdtls/eclipse.jdt.ls",
                                                            new List<LSPLanguage> { Java },
                                                            "jdtls");

        public static readonly LSPServer TypescriptLanguageServer = new("Typescript Language Server",
                                                                        "https://github.com/typescript-language-server/typescript-language-server",
                                                                        new List<LSPLanguage> { TypeScript, JavaScript },
                                                                        "typescript-language-server", "--stdio");

        public static readonly LSPServer VscodeJson = new("VSCode JSON Language Server",
                                                          "https://www.npmjs.com/package/vscode-json-languageserver",
                                                          new List<LSPLanguage> { JSON },
                                                          "vscode-json-languageserver", "--stdio");

        public static readonly LSPServer Texlab = new("Texlab",
                                                      "https://github.com/latex-lsp/texlab",
                                                      new List<LSPLanguage> { LaTeX },
                                                      "texlab");

        public static readonly LSPServer LuaLanguageServer = new("Lua Language Server",
                                                                 "https://github.com/LuaLS/lua-language-server",
                                                                 new List<LSPLanguage> { Lua },
                                                                 "lua-language-server");

        public static readonly LSPServer Marksman = new("Marksman",
                                                        "https://github.com/artempyanykh/marksman",
                                                        new List<LSPLanguage> { Markdown },
                                                        "marksman", "server");

        public static readonly LSPServer MatlabLanguageServer = new("Matlab Language Server",
                                                                    "https://github.com/mathworks/MATLAB-language-server",
                                                                    new List<LSPLanguage> { MATLAB },
                                                                    "matlab-language-server", "--stdio");

        public static readonly LSPServer PhpActor = new("Phpactor",
                                                        "https://github.com/phpactor/phpactor",
                                                        new List<LSPLanguage> { PHP },
                                                        "phpactor", "language-server");

        public static readonly LSPServer Intelephense = new("Intelephense",
                                                            "https://github.com/bmewburn/vscode-intelephense",
                                                            new List<LSPLanguage> { PHP },
                                                            "intelephense", "--stdio");

        public static readonly LSPServer Omnisharp = new("Omnisharp",
                                                         "https://github.com/OmniSharp/omnisharp-roslyn",
                                                         new List<LSPLanguage> { CSharp },
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
                                                                  new List<LSPLanguage> { Dart },
                                                                  "dart", "language-server");

        public static readonly LSPServer KotlinLanguageServer = new("Kotlin Language Server",
                                                                    "https://github.com/fwcd/kotlin-language-server",
                                                                    new List<LSPLanguage> { Kotlin },
                                                                    "kotlin-language-server");

        public static readonly LSPServer Pyright = new("Pyright",
                                                       "https://github.com/microsoft/pyright",
                                                       new List<LSPLanguage> { Python },
                                                       "pyright-langserver",
                                                       "--stdio");

        public static readonly LSPServer Jedi = new("Jedi Language Server",
                                                    "https://github.com/pappasam/jedi-language-server",
                                                    new List<LSPLanguage> { Python },
                                                    "jedi-language-server");

        public static readonly LSPServer RubyLsp = new("Ruby LSP",
                                                       "https://github.com/Shopify/ruby-lsp",
                                                       new List<LSPLanguage> { Ruby },
                                                       "srb", "typecheck --lsp --disable-watchman .");

        public static readonly LSPServer RustAnalyzer = new("Rust Analyzer",
                                                            "https://github.com/rust-lang/rust-analyzer",
                                                            new List<LSPLanguage> { Rust },
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

        public static readonly LSPServer Lemminx = new("Lemminx",
                                                       "https://github.com/eclipse/lemminx",
                                                       new List<LSPLanguage> { XML },
                                                       "lemminx");

        public static readonly LSPServer ZLS = new("ZLS",
                                                   "https://github.com/zigtools/zls",
                                                   new List<LSPLanguage> { Zig },
                                                   "zls");

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
