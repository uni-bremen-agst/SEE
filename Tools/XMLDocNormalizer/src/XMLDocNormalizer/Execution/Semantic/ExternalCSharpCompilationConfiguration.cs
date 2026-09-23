using Microsoft.CodeAnalysis.CSharp;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Holds the C# parse and compilation options that can be reconstructed
    /// safely from validated Portable PDB compilation-option provenance.
    /// </summary>
    /// <remarks>
    /// This result is intended for semantic source reconstruction. It is not
    /// a complete or bit-identical copy of the original compiler invocation.
    /// Unsupported semantic options cause reconstruction to fail instead of
    /// being ignored.
    /// </remarks>
    internal sealed class ExternalCSharpCompilationConfiguration
    {
        /// <summary>
        /// Initializes a reconstructed C# analysis configuration.
        /// </summary>
        /// <param name="parseOptions">The reconstructed parse options.</param>
        /// <param name="compilationOptions">
        /// The reconstructed compilation options.
        /// </param>
        /// <param name="compilerVersion">
        /// The exact compiler-version provenance string.
        /// </param>
        /// <param name="runtimeVersion">
        /// The exact runtime-version provenance string.
        /// </param>
        /// <param name="sourceFileCount">
        /// The recorded number of source files.
        /// </param>
        /// <param name="defaultEncodingWebName">
        /// The recorded default source encoding web name, when present.
        /// </param>
        /// <param name="fallbackEncodingWebName">
        /// The recorded fallback source encoding web name, when present.
        /// </param>
        internal ExternalCSharpCompilationConfiguration(
            CSharpParseOptions parseOptions,
            CSharpCompilationOptions compilationOptions,
            string compilerVersion,
            string runtimeVersion,
            int sourceFileCount,
            string? defaultEncodingWebName,
            string? fallbackEncodingWebName)
        {
            ParseOptions = parseOptions;
            CompilationOptions = compilationOptions;
            SigningProvenance = ExternalAssemblySigningProvenance.Unsigned;
            CompilerVersion = compilerVersion;
            RuntimeVersion = runtimeVersion;
            SourceFileCount = sourceFileCount;
            DefaultEncodingWebName = defaultEncodingWebName;
            FallbackEncodingWebName = fallbackEncodingWebName;
        }

        /// <summary>Initializes a reconstructed C# analysis configuration.</summary>
        /// <param name="parseOptions">The reconstructed parse options.</param>
        /// <param name="compilationOptions">The reconstructed compilation options.</param>
        /// <param name="signingProvenance">The exact target PE signing provenance.</param>
        /// <param name="compilerVersion">The exact compiler-version provenance.</param>
        /// <param name="runtimeVersion">The exact runtime-version provenance.</param>
        /// <param name="sourceFileCount">The recorded source-file count.</param>
        /// <param name="defaultEncodingWebName">The optional default encoding name.</param>
        /// <param name="fallbackEncodingWebName">The optional fallback encoding name.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="signingProvenance"/> is <see langword="null"/>.
        /// </exception>
        internal ExternalCSharpCompilationConfiguration(
            CSharpParseOptions parseOptions,
            CSharpCompilationOptions compilationOptions,
            ExternalAssemblySigningProvenance signingProvenance,
            string compilerVersion,
            string runtimeVersion,
            int sourceFileCount,
            string? defaultEncodingWebName,
            string? fallbackEncodingWebName)
        {
            ArgumentNullException.ThrowIfNull(signingProvenance);
            ParseOptions = parseOptions;
            CompilationOptions = compilationOptions;
            SigningProvenance = signingProvenance;
            CompilerVersion = compilerVersion;
            RuntimeVersion = runtimeVersion;
            SourceFileCount = sourceFileCount;
            DefaultEncodingWebName = defaultEncodingWebName;
            FallbackEncodingWebName = fallbackEncodingWebName;
        }

        /// <summary>
        /// Gets the reconstructed C# parse options for semantic analysis.
        /// </summary>
        /// <value>
        /// Options with the recorded concrete language version and symbols.
        /// <see cref="Microsoft.CodeAnalysis.DocumentationMode.Parse"/> and
        /// <see cref="Microsoft.CodeAnalysis.SourceCodeKind.Regular"/> are
        /// explicit analysis policies, not claims about original switches.
        /// </value>
        public CSharpParseOptions ParseOptions { get; }

        /// <summary>
        /// Gets the supported reconstructed C# compilation options.
        /// </summary>
        /// <value>
        /// Options suitable for semantic analysis. A validated complete public
        /// key may reconstruct signed assembly identity. Warning, private-key,
        /// emit, resolver, and other unserialized settings retain analysis
        /// defaults and are not claimed to match the original invocation.
        /// </value>
        public CSharpCompilationOptions CompilationOptions { get; }

        /// <summary>Gets the exact target PE signing provenance.</summary>
        /// <value>
        /// The signing state from P4A used to reconstruct semantic identity.
        /// No private key or historical signature is reproduced.
        /// </value>
        public ExternalAssemblySigningProvenance SigningProvenance { get; }

        /// <summary>
        /// Gets the exact recorded compiler-version provenance.
        /// </summary>
        /// <value>The unmodified Portable PDB value.</value>
        public string CompilerVersion { get; }

        /// <summary>
        /// Gets the exact recorded runtime-version provenance.
        /// </summary>
        /// <value>The unmodified Portable PDB value.</value>
        public string RuntimeVersion { get; }

        /// <summary>
        /// Gets the recorded source-file count.
        /// </summary>
        /// <value>
        /// The nonnegative Portable PDB value without speculative document
        /// count cross-validation.
        /// </value>
        public int SourceFileCount { get; }

        /// <summary>
        /// Gets the recorded default source encoding web name.
        /// </summary>
        /// <value>
        /// The exact web name, or <see langword="null"/> when absent. No
        /// environment-dependent encoding resolution has been performed.
        /// </value>
        public string? DefaultEncodingWebName { get; }

        /// <summary>
        /// Gets the recorded fallback source encoding web name.
        /// </summary>
        /// <value>
        /// The exact web name, or <see langword="null"/> when absent. No
        /// environment-dependent encoding resolution has been performed.
        /// </value>
        public string? FallbackEncodingWebName { get; }
    }
}
