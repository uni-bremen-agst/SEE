using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests external XML-documentation contracts in exception-flow
    /// analysis.
    /// </summary>
    public sealed class ExceptionFlowExternalDocumentationTests
    {
        /// <summary>
        /// Ensures that documented external exceptions contribute only to
        /// transitive exception flow.
        /// </summary>
        [Fact]
        public void ExternalDocumentationContract_IsTransitiveOnly()
        {
            PortableExecutableReference externalReference =
                CreateExternalReference();

            const string source =
                """
                using ExternalContracts;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        ExternalApi.Execute();
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun directRun =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeDirectly(
                        source,
                        "M",
                        externalReference);

            ExceptionFlowAnalyzerTestRun projectRun =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeTransitively(
                        source,
                        "M",
                        externalReference);

            ExceptionFlowAnalyzerTestRun solutionRun =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeSolutionTransitively(
                        source,
                        "M",
                        externalReference);

            AssertContractAbsent(
                directRun,
                "System.IO.IOException");

            AssertContractPresent(
                projectRun,
                "System.IO.IOException");

            AssertContractPresent(
                solutionRun,
                "System.IO.IOException");
        }

        /// <summary>
        /// Ensures that multiple documented exception contracts are
        /// propagated by both transitive engines.
        /// </summary>
        [Fact]
        public void MultipleExternalContracts_ArePropagated()
        {
            PortableExecutableReference externalReference =
                CreateExternalReference();

            const string source =
                """
                using ExternalContracts;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        ExternalApi.Execute();
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun projectRun =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeTransitively(
                        source,
                        "M",
                        externalReference);

            ExceptionFlowAnalyzerTestRun solutionRun =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeSolutionTransitively(
                        source,
                        "M",
                        externalReference);

            AssertContractPresent(
                projectRun,
                "System.IO.IOException");

            AssertContractPresent(
                projectRun,
                "System.InvalidOperationException");

            AssertContractPresent(
                solutionRun,
                "System.IO.IOException");

            AssertContractPresent(
                solutionRun,
                "System.InvalidOperationException");
        }

        /// <summary>
        /// Ensures that external documentation remains partial knowledge and
        /// does not make the unavailable method body fully analyzable.
        /// </summary>
        [Fact]
        public void ExternalDocumentationContract_PreservesUncertainty()
        {
            PortableExecutableReference externalReference =
                CreateExternalReference();

            const string source =
                """
                using ExternalContracts;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        ExternalApi.Execute();
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun projectRun =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeTransitively(
                        source,
                        "M",
                        externalReference);

            ExceptionFlowAnalyzerTestRun solutionRun =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeSolutionTransitively(
                        source,
                        "M",
                        externalReference);

            Assert.True(
                projectRun.Result.HasUncertainPaths);

            Assert.True(
                solutionRun.Result.HasUncertainPaths);
        }

        /// <summary>
        /// Creates an external assembly with two documented exceptions.
        /// </summary>
        /// <returns>
        /// The external metadata reference.
        /// </returns>
        private static PortableExecutableReference
            CreateExternalReference()
        {
            const string source =
                """
                namespace ExternalContracts
                {
                    public static class ExternalApi
                    {
                        /// <summary>Executes external work.</summary>
                        /// <exception cref="System.IO.IOException">
                        /// Thrown when an I/O operation fails.
                        /// </exception>
                        /// <exception cref="System.InvalidOperationException">
                        /// Thrown when the operation is invalid.
                        /// </exception>
                        public static void Execute()
                        {
                        }
                    }
                }
                """;

            return ExternalDocumentationReferenceTestHelper
                .Create(
                    source);
        }

        /// <summary>
        /// Asserts that neither proven flow nor external-documentation evidence
        /// exists for the specified exception type.
        /// </summary>
        /// <param name="run">
        /// The completed analyzer run.
        /// </param>
        /// <param name="metadataName">
        /// The expected exception metadata name.
        /// </param>
        private static void AssertContractAbsent(
            ExceptionFlowAnalyzerTestRun run,
            string metadataName)
        {
            INamedTypeSymbol exceptionType =
                run.GetRequiredType(
                    metadataName);

            Assert.Empty(
                run.Result.GetExceptionPaths(
                    exceptionType));

            Assert.Empty(
                run.Result
                    .GetExternalDocumentationEvidencePaths(
                        exceptionType));
        }

        /// <summary>
        /// Asserts that the specified exception is present as external
        /// documentation evidence but not as a proven exception.
        /// </summary>
        /// <param name="run">
        /// The completed analyzer run.
        /// </param>
        /// <param name="metadataName">
        /// The expected exception metadata name.
        /// </param>
        private static void AssertContractPresent(
            ExceptionFlowAnalyzerTestRun run,
            string metadataName)
        {
            INamedTypeSymbol exceptionType =
                run.GetRequiredType(
                    metadataName);

            Assert.Empty(
                run.Result.GetExceptionPaths(
                    exceptionType));

            IReadOnlyList<ExceptionFlowPath> evidencePaths =
                run.Result
                    .GetExternalDocumentationEvidencePaths(
                        exceptionType);

            Assert.NotEmpty(
                evidencePaths);

            Assert.Contains(
                evidencePaths,
                path =>
                    path.Steps.Any(
                        step =>
                            step.Kind ==
                            ExceptionFlowPathStepKind
                                .ExternalDocumentationEvidence));
        }
    }
}
