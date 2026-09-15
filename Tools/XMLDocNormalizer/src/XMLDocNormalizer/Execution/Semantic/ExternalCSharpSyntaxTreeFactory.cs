using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Decodes checksum-validated P5H source bytes and parses one C# syntax
    /// tree with the exact P5G parse options.
    /// </summary>
    internal static class ExternalCSharpSyntaxTreeFactory
    {
        /// <summary>
        /// UTF-8 without a byte-order mark, used when BOM-less source has no
        /// recorded default or fallback encoding.
        /// </summary>
        private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);

        /// <summary>
        /// Tries to reconstruct one C# source text and syntax tree without
        /// performing source acquisition or other I/O.
        /// </summary>
        /// <param name="material">
        /// The P5H source material containing the only source byte input.
        /// </param>
        /// <param name="configuration">
        /// The P5G configuration supplying encoding provenance and the exact
        /// parse options.
        /// </param>
        /// <param name="sourceTree">
        /// The decoded source text and parsed C# syntax tree when successful.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the document is C#, its checksum
        /// algorithm is exactly representable by Roslyn, its effective
        /// encoding can be resolved, and the decoded text retains the original
        /// checksum; otherwise <see langword="false"/>.
        /// </returns>
        /// <remarks>
        /// Syntax diagnostics do not cause failure. The current Roslyn parser
        /// can legitimately diagnose source produced by another compiler
        /// version. The document name is used only as opaque syntax-tree path
        /// provenance and is never opened as a file.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="material"/> or
        /// <paramref name="configuration"/> is <see langword="null"/>.
        /// </exception>
        public static bool TryCreate(
            ValidatedExternalSourceMaterial material,
            ExternalCSharpCompilationConfiguration configuration,
            out ExternalCSharpSyntaxTree sourceTree)
        {
            ArgumentNullException.ThrowIfNull(material);
            ArgumentNullException.ThrowIfNull(configuration);

            ExternalSourceDocumentDescriptor document = material.Document;

            if (document.Language != ExternalSourceDocumentLanguageIdentifiers.CSharp
                || !ExternalSourceDocumentChecksumValidator.TryGetRoslynSourceHashAlgorithm(
                    document.HashAlgorithm,
                    out SourceHashAlgorithm checksumAlgorithm)
                || !TrySelectEncoding(
                    material.Image.AsSpan(),
                    configuration,
                    out Encoding encoding))
            {
                sourceTree = null!;
                return false;
            }

            // P5H owns this immutable array. Roslyn receives it read-only and
            // the factory never mutates the exposed backing storage.
            byte[]? sourceBytes = ImmutableCollectionsMarshal.AsArray(material.Image);

            if (sourceBytes == null)
            {
                sourceTree = null!;
                return false;
            }

            SourceText text = SourceText.From(
                sourceBytes,
                sourceBytes.Length,
                encoding,
                checksumAlgorithm,
                throwIfBinaryDetected: false,
                canBeEmbedded: false);

            if (text.Encoding == null
                || text.ChecksumAlgorithm != checksumAlgorithm
                || !text.GetChecksum().AsSpan().SequenceEqual(document.Hash.AsSpan()))
            {
                sourceTree = null!;
                return false;
            }

            SyntaxTree tree = CSharpSyntaxTree.ParseText(
                text,
                configuration.ParseOptions,
                document.Name);
            sourceTree = new ExternalCSharpSyntaxTree(document, text, tree);
            return true;
        }

        /// <summary>
        /// Selects the BOM encoding or the configured BOM-less fallback.
        /// </summary>
        /// <param name="sourceBytes">The validated source bytes.</param>
        /// <param name="configuration">The P5G encoding provenance.</param>
        /// <param name="encoding">The concrete decoding encoding.</param>
        /// <returns>
        /// <see langword="true"/> when the effective encoding is available;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool TrySelectEncoding(
            ReadOnlySpan<byte> sourceBytes,
            ExternalCSharpCompilationConfiguration configuration,
            out Encoding encoding)
        {
            if (HasUtf32Bom(sourceBytes))
            {
                encoding = null!;
                return false;
            }

            if (TryGetBomEncoding(sourceBytes, out encoding))
            {
                return true;
            }

            string? webName = configuration.DefaultEncodingWebName
                ?? configuration.FallbackEncodingWebName;

            if (webName == null)
            {
                encoding = Utf8WithoutBom;
                return true;
            }

            return TryResolveEncoding(webName, out encoding);
        }

        /// <summary>
        /// Recognizes Unicode byte-order marks supported exactly by Roslyn's
        /// byte-array source-text decoder without removing their bytes.
        /// </summary>
        /// <param name="sourceBytes">The validated source bytes.</param>
        /// <param name="encoding">The encoding identified by the BOM.</param>
        /// <returns>
        /// <see langword="true"/> for UTF-8 or UTF-16 little- or big-endian
        /// BOMs; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool TryGetBomEncoding(
            ReadOnlySpan<byte> sourceBytes,
            out Encoding encoding)
        {
            if (sourceBytes.Length >= 3
                && sourceBytes[0] == 0xef
                && sourceBytes[1] == 0xbb
                && sourceBytes[2] == 0xbf)
            {
                encoding = Encoding.UTF8;
                return true;
            }

            if (sourceBytes.Length >= 2
                && sourceBytes[0] == 0xff
                && sourceBytes[1] == 0xfe)
            {
                encoding = Encoding.Unicode;
                return true;
            }

            if (sourceBytes.Length >= 2
                && sourceBytes[0] == 0xfe
                && sourceBytes[1] == 0xff)
            {
                encoding = Encoding.BigEndianUnicode;
                return true;
            }

            encoding = null!;
            return false;
        }

        /// <summary>
        /// Detects UTF-32 BOMs that Roslyn 5.0 cannot decode exactly through
        /// the original-byte <see cref="SourceText.From(byte[], int, Encoding,
        /// SourceHashAlgorithm, bool, bool)"/> API.
        /// </summary>
        /// <param name="sourceBytes">The validated source bytes.</param>
        /// <returns>
        /// <see langword="true"/> for UTF-32 little- or big-endian BOMs;
        /// otherwise <see langword="false"/>.
        /// </returns>
        private static bool HasUtf32Bom(ReadOnlySpan<byte> sourceBytes)
        {
            return sourceBytes.Length >= 4
                && ((sourceBytes[0] == 0xff
                     && sourceBytes[1] == 0xfe
                     && sourceBytes[2] == 0x00
                     && sourceBytes[3] == 0x00)
                    || (sourceBytes[0] == 0x00
                        && sourceBytes[1] == 0x00
                        && sourceBytes[2] == 0xfe
                        && sourceBytes[3] == 0xff));
        }

        /// <summary>
        /// Resolves one recorded web name without mutating the process-wide
        /// encoding provider registry.
        /// </summary>
        /// <param name="webName">The exact recorded encoding web name.</param>
        /// <param name="encoding">The resolved runtime encoding.</param>
        /// <returns>
        /// <see langword="true"/> when the core runtime or direct code-pages
        /// provider recognizes the web name; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool TryResolveEncoding(string webName, out Encoding encoding)
        {
            try
            {
                encoding = Encoding.GetEncoding(webName);
                return true;
            }
            catch (ArgumentException)
            {
            }

            try
            {
                Encoding? codePageEncoding = CodePagesEncodingProvider.Instance
                    .GetEncoding(webName);

                if (codePageEncoding != null)
                {
                    encoding = codePageEncoding;
                    return true;
                }
            }
            catch (ArgumentException)
            {
            }

            encoding = null!;
            return false;
        }
    }
}
