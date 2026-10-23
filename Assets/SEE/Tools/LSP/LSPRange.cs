using Range = SEE.Graphs.Range;

namespace SEE.Tools.LSP
{
    /// <summary>
    /// Conversion of ranges of the Language Server Protocol into ranges of SEE.
    /// </summary>
    internal static class LSPRange
    {
        /// <summary>
        /// Converts the given <paramref name="lspRange"/> (i.e., from OmniSharp) to a <see cref="Range"/> of SEE.
        /// </summary>
        /// <param name="lspRange">The LSP range to convert.</param>
        /// <returns>The converted range.</returns>
        public static Range FromLspRange(OmniSharp.Extensions.LanguageServer.Protocol.Models.Range lspRange)
        {
            if (lspRange == null)
            {
                return null;
            }
            return new Range(lspRange.Start.Line+1, lspRange.End.Line+1,
                             lspRange.Start.Character+1, lspRange.End.Character+1);
        }
    }
}
