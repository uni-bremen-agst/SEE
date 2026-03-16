using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizerTests.Configuration
{
    /// <summary>
    /// Tests default values of <see cref="XmlDocOptions"/>.
    /// </summary>
    public sealed class XmlDocOptionsTests
    {
        /// <summary>
        /// Ensures that the default exception analysis mode is ProjectTransitiveProjectExceptions.
        /// </summary>
        [Fact]
        public void DefaultExceptionAnalysisMode_IsProjectTransitiveProjectExceptions()
        {
            XmlDocOptions options = new();

            Assert.Equal(
                ExceptionAnalysisMode.ProjectTransitiveProjectExceptions,
                options.ExceptionAnalysisMode);
        }
    }
}
