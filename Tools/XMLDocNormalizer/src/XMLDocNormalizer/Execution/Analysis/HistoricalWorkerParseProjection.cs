using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.HistoricalWorker;

namespace XMLDocNormalizer.Execution.Analysis
{
    /// <summary>Primitive parse projection after the caller checked the original P5G/P5I receipt.</summary>
    internal static class HistoricalWorkerParseProjection
    {
        internal static bool TryProject(ExternalCSharpCompilationConfiguration configuration, out WorkerParseContext? context)
        {
            var parse = configuration.ParseOptions;
            context = null;
            if (parse.Kind != SourceCodeKind.Regular || parse.DocumentationMode != DocumentationMode.Parse
                || parse.Features.Any() || parse.SpecifiedLanguageVersion is LanguageVersion.Default
                    or LanguageVersion.Latest or LanguageVersion.LatestMajor or LanguageVersion.Preview)
            { return false; }
            // Roslyn displays major versions as "12.0"; use the same concrete logical version's
            // canonical major notation, not a default, downgrade or compiler-behavior emulation.
            string language = parse.SpecifiedLanguageVersion.ToDisplayString();
            if (language.EndsWith(".0", StringComparison.Ordinal)) { language = language[..^2]; }
            var value = new WorkerParseContext(language,
                parse.PreprocessorSymbolNames.ToArray(), "parse", "regular");
            if (!WorkerInputValidation.HasSupportedParseContext(new(ReferenceProfile: WorkerProtocol.ExactReferenceProfile,
                LanguageVersion: value.LanguageVersion!, ParseContext: value, ParseProfile: "validated-parse-v1")))
            { return false; }
            context = value;
            return true;
        }
    }
}
