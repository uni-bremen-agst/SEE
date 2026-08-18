using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests DOC631 behavior for complete runtime dispatch inside one project.
    /// </summary>
    public sealed class DOC631_ProjectTransitiveInternalDispatchTests
    {
        /// <summary>
        /// Ensures that a documented exception reached through a completely
        /// known internal interface dispatch does not produce DOC631.
        /// </summary>
        [Fact]
        public void InternalInterfaceDispatch_WithProvenException_DoesNotProduceDoc631()
        {
            const string source =
                """
                #nullable enable
                using System;

                internal interface IReporter
                {
                    void Complete(Result? result);
                }

                internal sealed class Reporter : IReporter
                {
                    public void Complete(Result? result)
                    {
                        Metrics.From(result);
                    }
                }

                internal static class Metrics
                {
                    public static void From(Result? result)
                    {
                        ArgumentNullException.ThrowIfNull(result);
                    }
                }

                internal sealed class Result
                {
                }

                internal sealed class EntryPoint
                {
                    /// <summary>Completes reporting.</summary>
                    /// <exception cref="ArgumentNullException">
                    /// Thrown when the result is null.
                    /// </exception>
                    public void M(
                        IReporter reporter,
                        Result? result)
                    {
                        reporter.Complete(result);
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.DoesNotContain(
                findings,
                finding =>
                    finding.Smell.ID ==
                    XmlDocSmells.ExceptionFlowNotDecidable.ID);

            Assert.DoesNotContain(
                findings,
                finding =>
                    finding.Smell.ID ==
                    XmlDocSmells
                        .ExceptionTagWithoutTransitiveThrow
                        .ID);
        }
    }
}
