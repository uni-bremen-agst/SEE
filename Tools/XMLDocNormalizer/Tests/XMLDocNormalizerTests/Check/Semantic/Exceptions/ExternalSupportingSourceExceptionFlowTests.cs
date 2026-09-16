using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;
using SourceInput = XMLDocNormalizerTests.Execution.Semantic.ExternalCSharpReconstructionFidelityTests.SourceInput;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests demand-driven summary resolution through exact P6A external
    /// supporting-source registrations.
    /// </summary>
    public sealed class ExternalSupportingSourceExceptionFlowTests
    {
        /// <summary>
        /// Registration alone does not add summaries or exception evidence.
        /// </summary>
        [Fact]
        public void RegisteredButUnusedDependency_RemainsFindingNeutral()
        {
            const string consumer =
                "public static class Consumer { public static void M() { } }";
            SourceInput[] dependency =
            [
                Source("External.cs", "using System; public static class Api { " +
                    "public static void Throw() => throw new InvalidOperationException(); }")
            ];

            ExceptionFlowSummaryGraphTestRun registered =
                ExternalSupportingSourceExceptionFlowTestHelper.Build(
                    dependency, consumer, "M");
            ExceptionFlowSummaryGraphTestRun unregistered =
                ExternalSupportingSourceExceptionFlowTestHelper.Build(
                    dependency, consumer, "M", registerExternalSource: false);

            Assert.Equal(unregistered.Graph.Count, registered.Graph.Count);
            Assert.Empty(registered.RootSummary.CallEdges);
            Assert.Empty(registered.RootSummary.Sources);
        }

        /// <summary>
        /// An exact registration redirects an ordinary metadata call to its
        /// source body, while an unregistered call retains metadata fallback.
        /// </summary>
        [Fact]
        public void DirectThrow_ExactRegistrationUsesSource_UnregisteredFallsBack()
        {
            SourceInput[] dependency =
            [
                Source("External.cs", "using System; namespace External { " +
                    "public static class Api { public static void A() { " +
                    "throw new InvalidOperationException(); } } }")
            ];
            const string consumer =
                "public static class Consumer { public static void M() { External.Api.A(); } }";

            ExceptionFlowSummaryGraphTestRun registered = Build(dependency, consumer);
            ExceptionFlowSummaryCallEdge sourceEdge = Assert.Single(
                registered.RootSummary.CallEdges);
            ExceptionFlowSummary sourceSummary = registered.GetRequiredSummary(sourceEdge.Target);

            AssertSourceBacked(sourceEdge.Target.Symbol, sourceSummary);
            Assert.Contains(
                sourceSummary.Sources,
                source => source.ExceptionType.Name == "InvalidOperationException");

            ExceptionFlowSummaryGraphTestRun unregistered =
                ExternalSupportingSourceExceptionFlowTestHelper.Build(
                    dependency, consumer, "M", registerExternalSource: false);
            ExceptionFlowSummaryCallEdge metadataEdge = Assert.Single(
                unregistered.RootSummary.CallEdges);

            Assert.Empty(metadataEdge.Target.Symbol.DeclaringSyntaxReferences);
            Assert.False(unregistered.GetRequiredSummary(metadataEdge.Target).HasExecutableBody);
        }

        /// <summary>
        /// Existing summary traversal follows an external source call into a
        /// second method declared in another tree of the same compilation.
        /// </summary>
        [Fact]
        public void CrossTreeTransitiveThrow_UsesExistingSummaryGraph()
        {
            SourceInput[] dependency =
            [
                Source("A.cs", "namespace External { public static partial class Api { " +
                    "public static void A() { B(); } } }"),
                Source("B.cs", "using System; namespace External { public static partial class Api { " +
                    "private static void B() { throw new InvalidOperationException(); } } }")
            ];

            ExceptionFlowSummaryGraphTestRun run = Build(
                dependency,
                "public static class Consumer { public static void M() { External.Api.A(); } }");
            ExceptionFlowSummaryCallEdge aEdge = Assert.Single(run.RootSummary.CallEdges);
            ExceptionFlowSummaryCallEdge bEdge = Assert.Single(
                run.GetRequiredSummary(aEdge.Target).CallEdges);

            Assert.Equal("B", bEdge.Target.Symbol.Name);
            Assert.Equal("/_/B.cs", Assert.Single(
                bEdge.Target.Symbol.DeclaringSyntaxReferences).SyntaxTree.FilePath);
            Assert.Contains(
                run.GetRequiredSummary(bEdge.Target).Sources,
                source => source.ExceptionType.Name == "InvalidOperationException");
        }

        /// <summary>
        /// A source-backed external method can cross another exact registered
        /// binary boundary without any external-specific graph implementation.
        /// </summary>
        [Fact]
        public void MultipleExternalDependencies_ResolveAcrossBothRegistrations()
        {
            ExceptionFlowSummaryGraphTestRun run =
                ExternalSupportingSourceExceptionFlowTestHelper.BuildChain(
                    [Source("Downstream.cs", "using System; namespace Downstream { " +
                        "public static class Api { public static void B() { " +
                        "throw new InvalidOperationException(); } } }")],
                    [Source("Upstream.cs", "namespace Upstream { public static class Api { " +
                        "public static void A() { Downstream.Api.B(); } } }")],
                    "public static class Consumer { public static void M() { Upstream.Api.A(); } }",
                    "M");
            ExceptionFlowSummaryCallEdge first = Assert.Single(run.RootSummary.CallEdges);
            ExceptionFlowSummaryCallEdge second = Assert.Single(
                run.GetRequiredSummary(first.Target).CallEdges);

            AssertSourceBacked(first.Target.Symbol, run.GetRequiredSummary(first.Target));
            AssertSourceBacked(second.Target.Symbol, run.GetRequiredSummary(second.Target));
            Assert.Contains(
                run.GetRequiredSummary(second.Target).Sources,
                source => source.ExceptionType.Name == "InvalidOperationException");
        }

        /// <summary>
        /// Equal assembly identity with a different manifest MVID cannot
        /// redirect the referenced metadata method.
        /// </summary>
        [Fact]
        public void WrongMvid_RetainsMetadataFallback()
        {
            const string consumer =
                "public static class Consumer { public static void M() { External.Api.A(); } }";
            ExceptionFlowSummaryGraphTestRun run =
                ExternalSupportingSourceExceptionFlowTestHelper.BuildWithWrongMvid(
                    [Source("Referenced.cs", "namespace External { public static class Api { " +
                        "public static void A() { } } }")],
                    [Source("Registered.cs", "using System; namespace External { " +
                        "public static class Api { public static void A() { " +
                        "throw new InvalidOperationException(); } } }")],
                    consumer,
                    "M");
            ExceptionFlowSummaryCallEdge edge = Assert.Single(run.RootSummary.CallEdges);

            Assert.Empty(edge.Target.Symbol.DeclaringSyntaxReferences);
            Assert.False(run.GetRequiredSummary(edge.Target).HasExecutableBody);
        }

        /// <summary>
        /// An exact binary registration still fails closed when an emitted
        /// compiler-generated callable has no source-symbol counterpart.
        /// </summary>
        [Fact]
        public void RegisteredButUnresolvableMethod_FailsClosed()
        {
            Assert.False(
                ExternalSupportingSourceExceptionFlowTestHelper
                    .TryResolveRegisteredGeneratedMethod());
        }

        /// <summary>
        /// Generic construction is resolved by the existing cross-compilation
        /// symbol resolver rather than method-name matching.
        /// </summary>
        [Fact]
        public void GenericMethod_ResolvesToSourceDeclaration()
        {
            ExceptionFlowSummaryGraphTestRun run = Build(
                [Source("Generic.cs", "using System; namespace External { " +
                    "public static class Api { public static void A<T>() { " +
                    "throw new InvalidOperationException(); } } }")],
                "public static class Consumer { public static void M() { External.Api.A<string>(); } }");
            ExceptionFlowSummaryCallEdge edge = Assert.Single(run.RootSummary.CallEdges);

            Assert.True(Assert.IsAssignableFrom<IMethodSymbol>(edge.Target.Symbol).IsGenericMethod);
            AssertSourceBacked(edge.Target.Symbol, run.GetRequiredSummary(edge.Target));
        }

        /// <summary>
        /// Reduced extension syntax maps to the external unreduced source
        /// declaration and keeps its receiver-to-parameter mapping.
        /// </summary>
        [Fact]
        public void ExtensionMethod_ResolvesToUnreducedSourceDeclaration()
        {
            ExceptionFlowSummaryGraphTestRun run = Build(
                [Source("Extensions.cs", "using System; namespace External { " +
                    "public static class Extensions { public static void A(this string value) { " +
                    "throw new InvalidOperationException(); } } }")],
                "using External; public static class Consumer { " +
                    "public static void M() { \"value\".A(); } }");
            ExceptionFlowSummaryCallEdge edge = Assert.Single(run.RootSummary.CallEdges);
            IMethodSymbol method = Assert.IsAssignableFrom<IMethodSymbol>(edge.Target.Symbol);

            Assert.True(method.IsExtensionMethod);
            Assert.Null(method.ReducedFrom);
            AssertSourceBacked(method, run.GetRequiredSummary(edge.Target));
        }

        /// <summary>
        /// Constructor callables use the same canonical external source path.
        /// </summary>
        [Fact]
        public void Constructor_ResolvesToSourceDeclaration()
        {
            ExceptionFlowSummaryGraphTestRun run = Build(
                [Source("Value.cs", "using System; namespace External { public sealed class Value { " +
                    "public Value() { throw new InvalidOperationException(); } } }")],
                "public static class Consumer { public static void M() { _ = new External.Value(); } }");
            ExceptionFlowSummaryCallEdge edge = Assert.Single(run.RootSummary.CallEdges);
            IMethodSymbol constructor = Assert.IsAssignableFrom<IMethodSymbol>(
                edge.Target.Symbol);

            Assert.Equal(MethodKind.Constructor, constructor.MethodKind);
            AssertSourceBacked(edge.Target.Symbol, run.GetRequiredSummary(edge.Target));
        }

        /// <summary>
        /// Source/metadata canonicalization keeps an external recursion cycle
        /// finite and reuses its existing graph keys.
        /// </summary>
        [Fact]
        public void Recursion_UsesCanonicalSourceKeysWithoutLooping()
        {
            ExceptionFlowSummaryGraphTestRun run = Build(
                [Source("Cycle.cs", "namespace External { public static class Api { " +
                    "public static void A() { B(); } private static void B() { A(); } } }")],
                "public static class Consumer { public static void M() { External.Api.A(); } }");
            ExceptionFlowSummaryCallEdge aEntry = Assert.Single(run.RootSummary.CallEdges);
            ExceptionFlowSummaryCallEdge bEdge = Assert.Single(
                run.GetRequiredSummary(aEntry.Target).CallEdges);
            ExceptionFlowSummaryCallEdge aCycle = Assert.Single(
                run.GetRequiredSummary(bEdge.Target).CallEdges);

            Assert.Equal(3, run.Graph.Count);
            Assert.Equal(aEntry.Target, aCycle.Target);
        }

        /// <summary>
        /// Exact receiver evidence lets existing virtual dispatch select the
        /// external source override.
        /// </summary>
        [Fact]
        public void VirtualDispatch_UsesExistingExternalSourceOverrideResolution()
        {
            ExceptionFlowSummaryGraphTestRun run = Build(
                [Source("Dispatch.cs", "using System; namespace External { " +
                    "public class Base { public virtual void M() { } } " +
                    "public sealed class Derived : Base { public override void M() { " +
                    "throw new InvalidOperationException(); } } }")],
                "public static class Consumer { public static void Run() { " +
                    "new External.Derived().M(); } }");
            ExceptionFlowSummaryCallEdge methodEdge = Assert.Single(
                run.RootSummary.CallEdges,
                edge => edge.Target.Symbol.Name == "M");

            Assert.Equal("Derived", methodEdge.Target.Symbol.ContainingType.Name);
            AssertSourceBacked(methodEdge.Target.Symbol, run.GetRequiredSummary(methodEdge.Target));
        }

        /// <summary>
        /// Source declarations without executable bodies do not invent throw
        /// evidence and retain conservative dispatch behavior.
        /// </summary>
        [Fact]
        public void InterfaceDeclarationWithoutImplementation_HasNoInventedBodyFlow()
        {
            ExceptionFlowSummaryGraphTestRun run = Build(
                [Source("Contract.cs", "namespace External { public interface IApi { void A(); } }")],
                "public static class Consumer { public static void M(External.IApi value) { value.A(); } }");

            Assert.Empty(run.RootSummary.Sources);

            foreach (ExceptionFlowSummaryCallEdge edge in run.RootSummary.CallEdges)
            {
                Assert.Empty(run.GetRequiredSummary(edge.Target).Sources);
                Assert.False(run.GetRequiredSummary(edge.Target).HasExecutableBody);
            }
        }

        /// <summary>
        /// Existing interface dispatch can discover a source implementation
        /// in the exact external supporting compilation.
        /// </summary>
        [Fact]
        public void InterfaceDispatch_UsesExistingExternalSourceImplementationResolution()
        {
            ExceptionFlowSummaryGraphTestRun run = Build(
                [Source("Interface.cs", "using System; namespace External { " +
                    "public interface IApi { void A(); } public sealed class Api : IApi { " +
                    "public void A() { throw new InvalidOperationException(); } } }")],
                "public static class Consumer { public static void M(External.Api value) { " +
                    "((External.IApi)value).A(); } }");
            ExceptionFlowSummaryCallEdge implementation = Assert.Single(
                run.RootSummary.CallEdges,
                edge => edge.Target.Symbol.ContainingType.Name == "Api");

            AssertSourceBacked(
                implementation.Target.Symbol,
                run.GetRequiredSummary(implementation.Target));
            Assert.Contains(
                run.GetRequiredSummary(implementation.Target).Sources,
                source => source.ExceptionType.Name == "InvalidOperationException");
        }

        /// <summary>
        /// Identical type and method names in different registered binaries
        /// retain separate symbol/compilation summary identities.
        /// </summary>
        [Fact]
        public void SameMemberNamesInDifferentBinaries_DoNotCollide()
        {
            ExceptionFlowSummaryGraphTestRun run =
                ExternalSupportingSourceExceptionFlowTestHelper.BuildAliasedPair(
                    [Source("First.cs", "using System; namespace Shared { public static class Api { " +
                        "public static void M() { throw new InvalidOperationException(); } } }")],
                    [Source("Second.cs", "using System; namespace Shared { public static class Api { " +
                        "public static void M() { throw new ArgumentOutOfRangeException(); } } }")],
                    "extern alias first; extern alias second; public static class Consumer { " +
                        "public static void Run() { first::Shared.Api.M(); second::Shared.Api.M(); } }",
                    "Run");
            ExceptionFlowSummaryCallEdge[] edges = run.RootSummary.CallEdges.ToArray();

            Assert.Equal(2, edges.Length);
            Assert.NotEqual(edges[0].Target, edges[1].Target);
            Assert.Contains(
                edges,
                edge => run.GetRequiredSummary(edge.Target).Sources.Any(
                    source => source.ExceptionType.Name == "InvalidOperationException"));
            Assert.Contains(
                edges,
                edge => run.GetRequiredSummary(edge.Target).Sources.Any(
                    source => source.ExceptionType.Name == "ArgumentOutOfRangeException"));
        }

        /// <summary>
        /// Existing property and operator callable paths also use the central
        /// external source canonicalization hook.
        /// </summary>
        [Theory]
        [InlineData("Property")]
        [InlineData("Operator")]
        public void ExistingCallableKinds_ResolveExternalSource(string callableKind)
        {
            const string dependency =
                "using System; namespace External { public sealed class Value { " +
                "public int Number { get { throw new InvalidOperationException(); } } " +
                "public static Value operator +(Value left, Value right) { " +
                "throw new InvalidOperationException(); } } }";
            string consumer = callableKind == "Property"
                ? "public static class Consumer { public static void M(External.Value value) { " +
                    "_ = value.Number; } }"
                : "public static class Consumer { public static void M(External.Value left, " +
                    "External.Value right) { _ = left + right; } }";
            ExceptionFlowSummaryGraphTestRun run = Build(
                [Source("CallableKinds.cs", dependency)],
                consumer);
            ExceptionFlowSummaryCallEdge edge = Assert.Single(run.RootSummary.CallEdges);

            AssertSourceBacked(edge.Target.Symbol, run.GetRequiredSummary(edge.Target));
            Assert.Contains(
                run.GetRequiredSummary(edge.Target).Sources,
                source => source.ExceptionType.Name == "InvalidOperationException");
        }

        /// <summary>
        /// A resolved extern declaration remains bodyless and contributes no
        /// invented exception source.
        /// </summary>
        [Fact]
        public void RegisteredExternMethod_RetainsBodylessFallback()
        {
            ExceptionFlowSummaryGraphTestRun run = Build(
                [Source("Native.cs", "using System.Runtime.InteropServices; namespace External { " +
                    "public static class Native { [DllImport(\"missing\")] public static extern void A(); } }")],
                "public static class Consumer { public static void M() { External.Native.A(); } }");
            ExceptionFlowSummaryCallEdge edge = Assert.Single(run.RootSummary.CallEdges);

            Assert.NotEmpty(edge.Target.Symbol.DeclaringSyntaxReferences);
            Assert.False(run.GetRequiredSummary(edge.Target).HasExecutableBody);
            Assert.Empty(run.GetRequiredSummary(edge.Target).Sources);
        }

        /// <summary>
        /// External source participation remains orthogonal to the four
        /// existing analysis modes: only body-transitive modes consume it.
        /// </summary>
        [Theory]
        [InlineData("Direct", false)]
        [InlineData("ProjectTransitiveDeclaredExceptions", false)]
        [InlineData("ProjectTransitive", true)]
        [InlineData("SolutionTransitive", true)]
        public void AnalysisModes_PreserveExistingTraversalSemantics(
            string modeName,
            bool expectsMissingTransitiveException)
        {
            ExceptionAnalysisMode mode = Enum.Parse<ExceptionAnalysisMode>(modeName);
            SourceInput[] dependency =
            [
                Source("Documented.cs", "using System; namespace External { " +
                    "public static class Api {\n/// <summary>External.</summary>\n" +
                    "/// <exception cref=\"ArgumentException\">Wrong contract.</exception>\n" +
                    "public static void A() { throw new InvalidOperationException(); } } }")
            ];
            const string consumer =
                "public static class Consumer {\n/// <summary>Runs.</summary>\n" +
                "public static void M() { External.Api.A(); } }";

            List<Finding> unregistered =
                ExternalSupportingSourceExceptionFlowTestHelper.Find(
                    dependency,
                    consumer,
                    mode,
                    registerExternalSource: false);
            List<Finding> registered =
                ExternalSupportingSourceExceptionFlowTestHelper.Find(
                    dependency,
                    consumer,
                    mode,
                    registerExternalSource: true);
            string missingId = XmlDocSmells.MissingTransitiveExceptionDocumentation.ID;

            Assert.Empty(unregistered);

            if (expectsMissingTransitiveException)
            {
                Finding finding = Assert.Single(registered);
                Assert.Equal(missingId, finding.Smell.ID);
                Assert.Contains("InvalidOperationException", finding.Message);
            }
            else
            {
                Assert.Empty(registered);
            }

            Assert.All(
                registered,
                finding => Assert.Equal(
                    ExceptionFlowAnalyzerTestHelper.SourcePath,
                    finding.FilePath));
        }

        private static ExceptionFlowSummaryGraphTestRun Build(
            IReadOnlyList<SourceInput> dependency,
            string consumer)
        {
            return ExternalSupportingSourceExceptionFlowTestHelper.Build(
                dependency,
                consumer,
                consumer.Contains(" Run()", StringComparison.Ordinal) ? "Run" : "M");
        }

        private static SourceInput Source(string fileName, string source)
        {
            return new SourceInput("/_/" + fileName, source);
        }

        private static void AssertSourceBacked(
            ISymbol symbol,
            ExceptionFlowSummary summary)
        {
            Assert.NotEmpty(symbol.DeclaringSyntaxReferences);
            Assert.True(summary.HasExecutableBody);
        }
    }
}
