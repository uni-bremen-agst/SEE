using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests precedence between executable supporting source and metadata
    /// exception evidence.
    /// </summary>
    public sealed class ExceptionFlowSupportingSourcePrecedenceTests
    {
        /// <summary>
        /// Uses the exception thrown by supporting source instead of the
        /// exception declared by external XML documentation.
        /// </summary>
        [Fact]
        public void SourceException_CompleteSourceCoverage_SuppressesXmlEvidence()
        {
            const string dependencySource =
                "namespace Dependency { public static class Service { " +
                "/// <exception cref=\"System.IO.IOException\">Failure.</exception>\n" +
                "public static void Execute() { throw new System.InvalidOperationException(); } } }";

            ExceptionFlowSummaryGraphTestRun run = Build(
                dependencySource,
                "public static class Consumer { public static void M() { Dependency.Service.Execute(); } }");
            ExceptionFlowSummaryCallEdge edge = Assert.Single(run.RootSummary.CallEdges);
            ExceptionFlowSummary targetSummary = run.GetRequiredSummary(edge.Target);

            AssertNoExternalDocumentationEvidence(run.RootSummary);
            Assert.True(targetSummary.HasExecutableBody);
            Assert.Contains(
                targetSummary.Sources,
                source => source.ExceptionType.Name == "InvalidOperationException");
            Assert.DoesNotContain(
                targetSummary.Sources,
                source => source.ExceptionType.Name == "IOException");
        }

        /// <summary>
        /// Treats an analyzable empty body as authoritative over external XML
        /// documentation.
        /// </summary>
        [Fact]
        public void EmptySourceBody_CompleteSourceCoverage_SuppressesXmlEvidence()
        {
            const string dependencySource =
                "namespace Dependency { public static class Service { " +
                "/// <exception cref=\"System.IO.IOException\">Failure.</exception>\n" +
                "public static void Execute() { } } }";

            ExceptionFlowSummaryGraphTestRun run = Build(
                dependencySource,
                "public static class Consumer { public static void M() { Dependency.Service.Execute(); } }");
            ExceptionFlowSummary targetSummary = run.GetRequiredSummary(
                Assert.Single(run.RootSummary.CallEdges).Target);

            AssertNoExternalDocumentationEvidence(run.RootSummary);
            Assert.True(targetSummary.HasExecutableBody);
            Assert.Empty(targetSummary.Sources);
            Assert.Empty(targetSummary.UncertainTargets);
        }

        /// <summary>
        /// Retains XML evidence when the resolved supporting declaration has
        /// no analyzable body.
        /// </summary>
        [Fact]
        public void BodylessSource_IncompleteSourceCoverage_RetainsXmlEvidence()
        {
            const string dependencySource =
                "namespace Dependency { public static class Service { " +
                "/// <exception cref=\"System.IO.IOException\">Failure.</exception>\n" +
                "[System.Runtime.InteropServices.DllImport(\"dependency\")] " +
                "public static extern void Execute(); } }";

            ExceptionFlowSummaryGraphTestRun run = Build(
                dependencySource,
                "public static class Consumer { public static void M() { Dependency.Service.Execute(); } }");
            ExceptionFlowSummaryCallEdge edge = Assert.Single(run.RootSummary.CallEdges);
            ExceptionFlowSummary targetSummary = run.GetRequiredSummary(edge.Target);

            AssertExternalDocumentationEvidence(run.RootSummary, "IOException");
            Assert.False(edge.Target.Symbol.DeclaringSyntaxReferences.IsDefaultOrEmpty);
            Assert.False(targetSummary.HasExecutableBody);
        }

        /// <summary>
        /// Keeps uncertainty from a metadata child call inside authoritative
        /// supporting source while suppressing the source method's XML evidence.
        /// </summary>
        [Fact]
        public void SourceWithUnknownChild_CompleteSourceCoverage_SuppressesOwnXmlEvidence()
        {
            const string dependencySource =
                "namespace Dependency { public static class Service { " +
                "/// <exception cref=\"System.IO.IOException\">Failure.</exception>\n" +
                "public static void Execute() { System.Console.WriteLine(); } } }";

            ExceptionFlowSummaryGraphTestRun run = Build(
                dependencySource,
                "public static class Consumer { public static void M() { Dependency.Service.Execute(); } }");
            ExceptionFlowSummary sourceSummary = run.GetRequiredSummary(
                Assert.Single(run.RootSummary.CallEdges).Target);
            ExceptionFlowSummaryCallEdge childEdge = Assert.Single(sourceSummary.CallEdges);
            ExceptionFlowSummary childSummary = run.GetRequiredSummary(childEdge.Target);

            AssertNoExternalDocumentationEvidence(run.RootSummary);
            Assert.True(sourceSummary.HasExecutableBody);
            Assert.True(childEdge.Target.Symbol.DeclaringSyntaxReferences.IsDefaultOrEmpty);
            Assert.False(childSummary.HasExecutableBody);
            Assert.Equal("WriteLine", childEdge.Target.Symbol.Name);
        }

        /// <summary>
        /// Preserves metadata-only XML evidence and unavailable-body behavior.
        /// </summary>
        [Fact]
        public void MetadataOnly_NoSourceCoverage_RetainsXmlEvidence()
        {
            const string dependencySource =
                "namespace Dependency { public static class Service { " +
                "/// <exception cref=\"System.IO.IOException\">Failure.</exception>\n" +
                "public static void Execute() { throw new System.InvalidOperationException(); } } }";
            const string consumerSource =
                "public static class Consumer { public static void M() { Dependency.Service.Execute(); } }";

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphProjectTestHelper.BuildWithExternalDocumentation(
                    dependencySource,
                    consumerSource,
                    "M");
            ExceptionFlowSummaryCallEdge edge = Assert.Single(run.RootSummary.CallEdges);
            ExceptionFlowSummary targetSummary = run.GetRequiredSummary(edge.Target);

            AssertExternalDocumentationEvidence(run.RootSummary, "IOException");
            Assert.True(edge.Target.Symbol.DeclaringSyntaxReferences.IsDefaultOrEmpty);
            Assert.False(targetSummary.HasExecutableBody);
            Assert.DoesNotContain(
                targetSummary.Sources,
                source => source.ExceptionType.Name == "InvalidOperationException");
        }

        /// <summary>
        /// Suppresses metadata evidence when exact-receiver dispatch is
        /// complete and every selected target has executable source.
        /// </summary>
        [Fact]
        public void ExactReceiverDispatch_CompleteSourceCoverage_SuppressesXmlEvidence()
        {
            const string dependencySource =
                "namespace Dependency { public class Service { " +
                "/// <exception cref=\"System.IO.IOException\">Failure.</exception>\n" +
                "public virtual void Execute() { throw new System.InvalidOperationException(); } } }";
            const string consumerSource =
                "public static class Consumer { public static void M() { new Dependency.Service().Execute(); } }";

            ExceptionFlowSummaryGraphTestRun run = Build(dependencySource, consumerSource);
            ExceptionFlowSummaryCallEdge edge = Assert.Single(
                run.RootSummary.CallEdges,
                callEdge => callEdge.CallSiteStep.Kind ==
                    ExceptionFlowPathStepKind.VirtualMethodCall);
            ExceptionFlowSummary targetSummary = run.GetRequiredSummary(edge.Target);

            Assert.Equal(ExceptionFlowPathStepKind.VirtualMethodCall, edge.CallSiteStep.Kind);
            AssertNoExternalDocumentationEvidence(run.RootSummary);
            Assert.Empty(run.RootSummary.UncertainTargets);
            Assert.True(targetSummary.HasExecutableBody);
            Assert.Contains(
                targetSummary.Sources,
                source => source.ExceptionType.Name == "InvalidOperationException");
        }

        /// <summary>
        /// Retains XML evidence and dispatch uncertainty when a public
        /// interface can have implementations outside the analyzed closure.
        /// </summary>
        [Fact]
        public void OpenInterfaceDispatch_IncompleteSourceCoverage_RetainsFallbacks()
        {
            const string dependencySource =
                "namespace Dependency { public interface IService { " +
                "/// <exception cref=\"System.IO.IOException\">Failure.</exception>\n" +
                "void Execute(); } public sealed class Service : IService { " +
                "public void Execute() { throw new System.InvalidOperationException(); } } }";
            const string consumerSource =
                "public static class Consumer { public static void M(Dependency.IService service) { " +
                "service.Execute(); } }";

            ExceptionFlowSummaryGraphTestRun run = Build(dependencySource, consumerSource);
            ExceptionFlowSummaryCallEdge edge = Assert.Single(run.RootSummary.CallEdges);
            ExceptionFlowSummary targetSummary = run.GetRequiredSummary(edge.Target);

            Assert.Equal(ExceptionFlowPathStepKind.InterfaceMethodCall, edge.CallSiteStep.Kind);
            AssertExternalDocumentationEvidence(run.RootSummary, "IOException");
            Assert.Single(run.RootSummary.UncertainTargets);
            Assert.True(targetSummary.HasExecutableBody);
            Assert.Contains(
                targetSummary.Sources,
                source => source.ExceptionType.Name == "InvalidOperationException");
        }

        /// <summary>
        /// Uses complete exact-receiver source coverage even though the
        /// selected interface assembly has no supporting-source scope.
        /// </summary>
        [Fact]
        public void ExactSourceImplementation_NoSelectedAssemblySupport_SuppressesXmlEvidence()
        {
            const string dependencySource =
                "namespace Dependency { public interface IService { " +
                "/// <exception cref=\"System.IO.IOException\">Failure.</exception>\n" +
                "void Execute(); } }";
            const string consumerSource =
                "public sealed class Service : Dependency.IService { " +
                "public void Execute() { throw new System.InvalidOperationException(); } } " +
                "public static class Consumer { public static void M() { " +
                "((Dependency.IService)new Service()).Execute(); } }";

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphProjectTestHelper.BuildWithExternalDocumentation(
                    dependencySource,
                    consumerSource,
                    "M");
            ExceptionFlowSummaryCallEdge edge = Assert.Single(
                run.RootSummary.CallEdges,
                callEdge => callEdge.CallSiteStep.Kind ==
                    ExceptionFlowPathStepKind.InterfaceMethodCall);
            ExceptionFlowSummary targetSummary = run.GetRequiredSummary(edge.Target);

            AssertNoExternalDocumentationEvidence(run.RootSummary);
            Assert.Empty(run.RootSummary.UncertainTargets);
            Assert.False(edge.Target.Symbol.DeclaringSyntaxReferences.IsDefaultOrEmpty);
            Assert.Equal("Service", edge.Target.Symbol.ContainingType.Name);
            Assert.True(targetSummary.HasExecutableBody);
            Assert.Contains(
                targetSummary.Sources,
                source => source.ExceptionType.Name == "InvalidOperationException");
        }

        /// <summary>
        /// Maps an inherited metadata runtime target to supporting source
        /// while retaining a separate source override in the closed set.
        /// </summary>
        [Fact]
        public void InheritedMetadataAndSourceOverride_CompleteCoverage_SuppressesXmlEvidence()
        {
            const string dependencySource =
                "namespace Dependency { public class BaseService { " +
                "/// <exception cref=\"System.IO.IOException\">Failure.</exception>\n" +
                "public virtual void Execute() { throw new System.ArgumentException(); } } }";
            const string consumerSource =
                "internal class LocalBase : Dependency.BaseService { } " +
                "internal sealed class LocalOverride : LocalBase { " +
                "public override void Execute() { throw new System.InvalidOperationException(); } } " +
                "internal static class Consumer { public static void M(LocalBase service) { " +
                "service.Execute(); } }";

            ExceptionFlowSummaryGraphTestRun run = Build(dependencySource, consumerSource);
            ExceptionFlowSummaryCallEdge[] edges = run.RootSummary.CallEdges
                .Where(
                    edge => edge.CallSiteStep.Kind ==
                        ExceptionFlowPathStepKind.VirtualMethodCall)
                .ToArray();

            Assert.Equal(2, edges.Length);
            Assert.Equal(3, run.Graph.Count);
            AssertNoExternalDocumentationEvidence(run.RootSummary);
            Assert.Empty(run.RootSummary.UncertainTargets);
            Assert.Contains(
                edges,
                edge =>
                    edge.Target.Symbol.ContainingType.Name == "BaseService"
                    && run.GetRequiredSummary(edge.Target).HasExecutableBody
                    && run.GetRequiredSummary(edge.Target).Sources.Any(
                        source => source.ExceptionType.Name == "ArgumentException"));
            Assert.Contains(
                edges,
                edge =>
                    edge.Target.Symbol.ContainingType.Name == "LocalOverride"
                    && run.GetRequiredSummary(edge.Target).HasExecutableBody
                    && run.GetRequiredSummary(edge.Target).Sources.Any(
                        source => source.ExceptionType.Name == "InvalidOperationException"));
        }

        /// <summary>
        /// Retains metadata evidence when a closed runtime target set contains
        /// an inherited metadata implementation without supporting source.
        /// </summary>
        [Fact]
        public void UnresolvedMetadataAndSourceOverride_IncompleteCoverage_RetainsXmlEvidence()
        {
            const string dependencySource =
                "namespace Dependency { public class BaseService { " +
                "/// <exception cref=\"System.IO.IOException\">Failure.</exception>\n" +
                "public virtual void Execute() { throw new System.ArgumentException(); } } }";
            const string consumerSource =
                "internal class LocalBase : Dependency.BaseService { } " +
                "internal sealed class LocalOverride : LocalBase { " +
                "public override void Execute() { throw new System.InvalidOperationException(); } } " +
                "internal static class Consumer { public static void M(LocalBase service) { " +
                "service.Execute(); } }";

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphProjectTestHelper.BuildWithExternalDocumentation(
                    dependencySource,
                    consumerSource,
                    "M");
            ExceptionFlowSummaryCallEdge edge = Assert.Single(
                run.RootSummary.CallEdges,
                callEdge => callEdge.CallSiteStep.Kind ==
                    ExceptionFlowPathStepKind.VirtualMethodCall);
            ExceptionFlowSummary targetSummary = run.GetRequiredSummary(edge.Target);

            AssertExternalDocumentationEvidence(run.RootSummary, "IOException");
            Assert.Empty(run.RootSummary.UncertainTargets);
            Assert.Equal("LocalOverride", edge.Target.Symbol.ContainingType.Name);
            Assert.True(targetSummary.HasExecutableBody);
            Assert.Contains(
                targetSummary.Sources,
                source => source.ExceptionType.Name == "InvalidOperationException");
        }

        /// <summary>
        /// Confirms that coverage uses the same body forms as the built
        /// summary.
        /// </summary>
        /// <param name="declaration">The supporting method declaration.</param>
        /// <param name="expectedExecutableBody">The expected summary body state.</param>
        [Theory]
        [InlineData("public static void Execute() { System.Console.WriteLine(); }", true)]
        [InlineData("public static void Execute() => System.Console.WriteLine();", true)]
        [InlineData("public static void Execute() { }", true)]
        [InlineData(
            "[System.Runtime.InteropServices.DllImport(\"dependency\")] public static extern void Execute();",
            false)]
        public void BodyAvailability_MatchesBuiltSummary(
            string declaration,
            bool expectedExecutableBody)
        {
            string dependencySource =
                "namespace Dependency { public static class Service { " +
                declaration +
                " } }";
            const string consumerSource =
                "public static class Consumer { public static void M() { Dependency.Service.Execute(); } }";

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphProjectTestHelper.BuildWithSupportingSource(
                    dependencySource,
                    consumerSource,
                    "M");
            ExceptionFlowSummary targetSummary = run.GetRequiredSummary(
                Assert.Single(run.RootSummary.CallEdges).Target);

            Assert.Equal(expectedExecutableBody, targetSummary.HasExecutableBody);
        }

        /// <summary>
        /// Builds the standard supporting-source and XML-documentation graph.
        /// </summary>
        /// <param name="dependencySource">The dependency source and documentation.</param>
        /// <param name="consumerSource">The consumer source.</param>
        /// <returns>The completed summary graph run.</returns>
        private static ExceptionFlowSummaryGraphTestRun Build(
            string dependencySource,
            string consumerSource)
        {
            return ExceptionFlowSummaryGraphProjectTestHelper
                .BuildWithSupportingSourceAndExternalDocumentation(
                    dependencySource,
                    consumerSource,
                    "M");
        }

        /// <summary>
        /// Asserts that a summary contains external documentation evidence for
        /// one exception type.
        /// </summary>
        /// <param name="summary">The summary to inspect.</param>
        /// <param name="exceptionTypeName">The expected exception type name.</param>
        private static void AssertExternalDocumentationEvidence(
            ExceptionFlowSummary summary,
            string exceptionTypeName)
        {
            Assert.Contains(
                summary.Sources,
                source =>
                    source.Kind == ExceptionFlowSourceKind.ExternalDocumentationEvidence
                    && source.ExceptionType.Name == exceptionTypeName);
        }

        /// <summary>
        /// Asserts that a summary contains no external documentation evidence.
        /// </summary>
        /// <param name="summary">The summary to inspect.</param>
        private static void AssertNoExternalDocumentationEvidence(
            ExceptionFlowSummary summary)
        {
            Assert.DoesNotContain(
                summary.Sources,
                source => source.Kind == ExceptionFlowSourceKind.ExternalDocumentationEvidence);
        }
    }
}
