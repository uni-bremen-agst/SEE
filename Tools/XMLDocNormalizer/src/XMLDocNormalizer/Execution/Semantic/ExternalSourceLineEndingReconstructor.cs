using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace XMLDocNormalizer.Execution.Semantic
{
    /// <summary>
    /// Produces a small deterministic set of byte-level line-ending candidates
    /// and delegates every acceptance decision to unchanged P5H validation.
    /// </summary>
    internal static class ExternalSourceLineEndingReconstructor
    {
        /// <summary>
        /// Strict UTF-8 validation used only to establish a supported byte
        /// form; candidate bytes are never decoded and re-encoded.
        /// </summary>
        private static readonly Encoding StrictUtf8 = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

        /// <summary>
        /// Tries bare-LF to CRLF and CRLF to LF in fixed order, returning only
        /// a candidate accepted by the existing P5H material factory.
        /// </summary>
        /// <param name="document">The validated Portable PDB document.</param>
        /// <param name="originalImage">The already acquired original bytes.</param>
        /// <param name="origin">The original controlled acquisition origin.</param>
        /// <param name="filePath">Optional original local-path provenance.</param>
        /// <param name="sourceIdentity">The original local path or credential-free URI.</param>
        /// <param name="configuration">The P5G encoding provenance.</param>
        /// <param name="material">The P5H-valid reconstructed material.</param>
        /// <param name="result">Bounded reconstruction work performed.</param>
        /// <returns>
        /// <see langword="true"/> only when one deterministic candidate passes
        /// the checksum recorded in <paramref name="document"/>.
        /// </returns>
        public static bool TryCreateValidatedMaterial(
            ExternalSourceDocumentDescriptor document,
            ImmutableArray<byte> originalImage,
            ExternalSourceMaterialOrigin origin,
            string? filePath,
            string sourceIdentity,
            ExternalCSharpCompilationConfiguration? configuration,
            out ValidatedExternalSourceMaterial material,
            out ExternalSourceReconstructionResult result)
        {
            if (document == null || sourceIdentity == null)
            {
                material = null!;
                result = default;
                return false;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            int candidateCount = 0;
            long bytesProduced = 0;

            if (originalImage.IsDefault
                || configuration == null
                || !ExternalCSharpSyntaxTreeFactory.TrySelectEncodingForSourceBytes(
                    originalImage.AsSpan(),
                    configuration,
                    out Encoding encoding)
                || encoding.CodePage != Encoding.UTF8.CodePage
                || !IsWellFormedUtf8(originalImage.AsSpan()))
            {
                stopwatch.Stop();
                material = null!;
                result = new ExternalSourceReconstructionResult(
                    candidateCount,
                    bytesProduced,
                    stopwatch.ElapsedTicks,
                    ExternalSourceLineEndingTransformation.None);
                return false;
            }

            if (TryConvertLfToCrlf(originalImage.AsSpan(), out ImmutableArray<byte> lfToCrlf))
            {
                candidateCount++;
                bytesProduced += lfToCrlf.Length;

                if (ValidatedExternalSourceMaterialFactory.TryCreateFromAcquiredImage(
                        document,
                        lfToCrlf,
                        origin,
                        filePath,
                        sourceIdentity,
                        ExternalSourceMaterialExactness.ReconstructedExact,
                        ExternalSourceLineEndingTransformation.LfToCrlf,
                        out material))
                {
                    stopwatch.Stop();
                    result = new ExternalSourceReconstructionResult(
                        candidateCount,
                        bytesProduced,
                        stopwatch.ElapsedTicks,
                        ExternalSourceLineEndingTransformation.LfToCrlf);
                    return true;
                }
            }

            if (TryConvertCrlfToLf(originalImage.AsSpan(), out ImmutableArray<byte> crlfToLf))
            {
                candidateCount++;
                bytesProduced += crlfToLf.Length;

                if (ValidatedExternalSourceMaterialFactory.TryCreateFromAcquiredImage(
                        document,
                        crlfToLf,
                        origin,
                        filePath,
                        sourceIdentity,
                        ExternalSourceMaterialExactness.ReconstructedExact,
                        ExternalSourceLineEndingTransformation.CrlfToLf,
                        out material))
                {
                    stopwatch.Stop();
                    result = new ExternalSourceReconstructionResult(
                        candidateCount,
                        bytesProduced,
                        stopwatch.ElapsedTicks,
                        ExternalSourceLineEndingTransformation.CrlfToLf);
                    return true;
                }
            }

            stopwatch.Stop();
            material = null!;
            result = new ExternalSourceReconstructionResult(
                candidateCount,
                bytesProduced,
                stopwatch.ElapsedTicks,
                ExternalSourceLineEndingTransformation.None);
            return false;
        }

        /// <summary>
        /// Validates UTF-8 syntax without changing any byte, including a BOM.
        /// </summary>
        /// <param name="image">The original acquired bytes.</param>
        /// <returns><see langword="true"/> only for well-formed UTF-8 bytes.</returns>
        private static bool IsWellFormedUtf8(ReadOnlySpan<byte> image)
        {
            try
            {
                _ = StrictUtf8.GetCharCount(image);
                return true;
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
        }

        /// <summary>
        /// Expands bare LF bytes while preserving existing CRLF and all other bytes.
        /// </summary>
        /// <param name="source">The original acquired bytes.</param>
        /// <param name="candidate">The transformed bytes when a bare LF exists.</param>
        /// <returns><see langword="true"/> when one bounded candidate was produced.</returns>
        private static bool TryConvertLfToCrlf(
            ReadOnlySpan<byte> source,
            out ImmutableArray<byte> candidate)
        {
            int bareLfCount = 0;

            for (int index = 0; index < source.Length; index++)
            {
                if (source[index] == (byte)'\n'
                    && (index == 0 || source[index - 1] != (byte)'\r'))
                {
                    bareLfCount++;
                }
            }

            if (bareLfCount == 0
                || source.Length > ExternalSourceLinkClient.DefaultMaximumResponseBytes - bareLfCount)
            {
                candidate = default;
                return false;
            }

            byte[] output = new byte[source.Length + bareLfCount];
            int outputIndex = 0;

            for (int index = 0; index < source.Length; index++)
            {
                byte value = source[index];

                if (value == (byte)'\n'
                    && (index == 0 || source[index - 1] != (byte)'\r'))
                {
                    output[outputIndex++] = (byte)'\r';
                }

                output[outputIndex++] = value;
            }

            candidate = ImmutableCollectionsMarshal.AsImmutableArray(output);
            return true;
        }

        /// <summary>
        /// Reduces CRLF pairs while preserving bare LF, bare CR, and all other bytes.
        /// </summary>
        /// <param name="source">The original acquired bytes.</param>
        /// <param name="candidate">The transformed bytes when a CRLF pair exists.</param>
        /// <returns><see langword="true"/> when one bounded candidate was produced.</returns>
        private static bool TryConvertCrlfToLf(
            ReadOnlySpan<byte> source,
            out ImmutableArray<byte> candidate)
        {
            int crlfCount = 0;

            for (int index = 0; index + 1 < source.Length; index++)
            {
                if (source[index] == (byte)'\r' && source[index + 1] == (byte)'\n')
                {
                    crlfCount++;
                    index++;
                }
            }

            if (crlfCount == 0)
            {
                candidate = default;
                return false;
            }

            byte[] output = new byte[source.Length - crlfCount];
            int outputIndex = 0;

            for (int index = 0; index < source.Length; index++)
            {
                if (source[index] == (byte)'\r'
                    && index + 1 < source.Length
                    && source[index + 1] == (byte)'\n')
                {
                    output[outputIndex++] = (byte)'\n';
                    index++;
                    continue;
                }

                output[outputIndex++] = source[index];
            }

            candidate = ImmutableCollectionsMarshal.AsImmutableArray(output);
            return true;
        }
    }

    /// <summary>
    /// Captures one bounded reconstruction attempt independently of whether a
    /// candidate passed P5H.
    /// </summary>
    /// <param name="CandidateCount">The number of generated candidates.</param>
    /// <param name="BytesProduced">The total generated candidate bytes.</param>
    /// <param name="DurationTicks">Elapsed reconstruction ticks.</param>
    /// <param name="SuccessfulTransformation">The accepted transformation, if any.</param>
    internal readonly record struct ExternalSourceReconstructionResult(
        int CandidateCount,
        long BytesProduced,
        long DurationTicks,
        ExternalSourceLineEndingTransformation SuccessfulTransformation);
}
