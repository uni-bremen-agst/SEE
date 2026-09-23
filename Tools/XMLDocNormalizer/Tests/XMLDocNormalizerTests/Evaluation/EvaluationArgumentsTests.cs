using XMLDocNormalizer.Evaluation;
using XMLDocNormalizer.Execution.Semantic;

namespace XMLDocNormalizerTests.Evaluation
{
    /// <summary>
    /// Tests the evaluation-only per-run source-reconstruction policy surface.
    /// </summary>
    public sealed class EvaluationArgumentsTests
    {
        [Fact]
        public void TryParse_OmittedReconstructionPolicy_DefaultsToStrict()
        {
            Assert.True(EvaluationArguments.TryParse(
                CreateArguments(),
                out EvaluationArguments options));

            Assert.Equal(
                ExternalSourceReconstructionPolicy.Strict,
                options.SourceReconstructionPolicy);
        }

        [Fact]
        public void TryParse_VerifiedLineEndings_SelectsOptInPolicy()
        {
            Assert.True(EvaluationArguments.TryParse(
                CreateArguments(
                    "--source-reconstruction",
                    "verified-line-endings"),
                out EvaluationArguments options));

            Assert.Equal(
                ExternalSourceReconstructionPolicy.VerifiedLineEndings,
                options.SourceReconstructionPolicy);
        }

        [Fact]
        public void TryParse_UnknownReconstructionPolicyFailsClosed()
        {
            Assert.False(EvaluationArguments.TryParse(
                CreateArguments("--source-reconstruction", "normalize"),
                out _));
        }

        private static string[] CreateArguments(params string[] additional)
        {
            return
            [
                "--manifest",
                "manifest.json",
                "--workspace",
                "workspace",
                "--output",
                "reports",
                .. additional
            ];
        }
    }
}
