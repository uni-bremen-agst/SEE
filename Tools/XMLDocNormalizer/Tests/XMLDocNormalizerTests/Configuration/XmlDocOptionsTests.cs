using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizerTests.Configuration
{
    /// <summary>
    /// Tests default values of XmlDocOptions.
    /// </summary>
    public sealed class XmlDocOptionsTests
    {
        /// <summary>
        /// Ensures that the central default exception analysis mode is solution-transitive.
        /// </summary>
        [Fact]
        public void DefaultExceptionAnalysisMode_IsSolutionTransitive()
        {
            Assert.Equal(
                ExceptionAnalysisMode.SolutionTransitive,
                XmlDocOptions.DefaultExceptionAnalysisMode);
        }

        /// <summary>
        /// Ensures that new options instances use the central default exception analysis mode.
        /// </summary>
        [Fact]
        public void NewOptions_UseDefaultExceptionAnalysisMode()
        {
            XmlDocOptions options = new();

            Assert.Equal(
                XmlDocOptions.DefaultExceptionAnalysisMode,
                options.ExceptionAnalysisMode);
        }
    }
}
