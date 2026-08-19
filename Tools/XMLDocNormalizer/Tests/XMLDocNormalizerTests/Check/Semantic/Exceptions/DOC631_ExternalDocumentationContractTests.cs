using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests DOC611 and DOC631 behavior for exception contracts obtained
    /// from external XML documentation.
    /// </summary>
    public sealed class DOC631_ExternalDocumentationContractTests
    {
        /// <summary>
        /// Ensures that a documented external exception is proven in
        /// project-transitive analysis.
        /// </summary>
        [Fact]
        public void DocumentedExternalEvidence_ProjectTransitive_RetainsDoc631()
        {
            AssertDocumentedExternalEvidenceRetainsDoc631(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures that a documented external exception is proven in
        /// solution-transitive analysis.
        /// </summary>
        [Fact]
        public void DocumentedExternalEvidence_SolutionTransitive_RetainsDoc631()
        {
            AssertDocumentedExternalEvidenceRetainsDoc631(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Ensures that missing documentation for an external exception
        /// produces DOC611 in project-transitive analysis.
        /// </summary>
        [Fact]
        public void ExternalEvidenceWithoutLocalDocumentation_ProjectTransitive_DoesNotProduceDoc611()
        {
            AssertExternalEvidenceWithoutLocalDocumentationDoesNotProduceDoc611(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures that missing documentation for an external exception
        /// produces DOC611 in solution-transitive analysis.
        /// </summary>
        [Fact]
        public void ExternalEvidenceWithoutLocalDocumentation_SolutionTransitive_DoesNotProduceDoc611()
        {
            AssertExternalEvidenceWithoutLocalDocumentationDoesNotProduceDoc611(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Verifies that a documented external exception does not produce
        /// DOC631.
        /// </summary>
        /// <param name="mode">
        /// The transitive analysis mode.
        /// </param>
        private static void
            AssertDocumentedExternalEvidenceRetainsDoc631(
                ExceptionAnalysisMode mode)
        {
            PortableExecutableReference externalReference =
                CreateExternalReference();

            const string source =
                """
                using System.IO;
                using ExternalContracts;

                public static class EntryPoint
                {
                    /// <summary>Executes external work.</summary>
                    /// <exception cref="IOException">
                    /// Thrown when the external operation encounters an
                    /// I/O error.
                    /// </exception>
                    public static void M()
                    {
                        ExternalApi.Execute();
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    mode,
                    externalReference);

            Finding finding =
                Assert.Single(findings.Where(candidate =>
                        candidate.Smell.ID == XmlDocSmells.ExceptionFlowNotDecidable.ID));

            Assert.Contains(
                "External XML documentation lists this exception",
                finding.Message,
                StringComparison.Ordinal);

            Assert.DoesNotContain(
                findings,
                candidate =>
                    candidate.Smell.ID ==
                    XmlDocSmells
                        .ExceptionTagWithoutTransitiveThrow
                        .ID);
        }

        /// <summary>
        /// Verifies that an undocumented external exception produces DOC611.
        /// </summary>
        /// <param name="mode">
        /// The transitive analysis mode.
        /// </param>
        private static void
            AssertExternalEvidenceWithoutLocalDocumentationDoesNotProduceDoc611(
                ExceptionAnalysisMode mode)
        {
            PortableExecutableReference externalReference =
                CreateExternalReference();

            const string source =
                """
                using ExternalContracts;

                public static class EntryPoint
                {
                    /// <summary>Executes external work.</summary>
                    public static void M()
                    {
                        ExternalApi.Execute();
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    mode,
                    externalReference);

            Assert.DoesNotContain(
                findings,
                candidate =>
                    candidate.Smell.ID ==
                    XmlDocSmells
                        .MissingTransitiveExceptionDocumentation
                        .ID);
        }

        /// <summary>
        /// Creates an external assembly with one documented exception.
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
    }
}
