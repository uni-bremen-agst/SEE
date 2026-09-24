using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exceptions
{
    /// <summary>
    /// Tests canonical summary transport and semantic equivalence.
    /// </summary>
    public sealed class CanonicalExceptionFlowTests
    {
        /// <summary>
        /// Supplies the eight required legacy-to-canonical equivalence fixtures.
        /// </summary>
        public static IEnumerable<object[]> EquivalenceFixtures()
        {
            yield return Fixture(
                "DirectThrow",
                "using System; class C { static void Root() { throw new InvalidOperationException(); } }");
            yield return Fixture(
                "TransitiveThrow",
                "using System; class C { static void Root() { Target(); } static void Target() { throw new InvalidOperationException(); } }");
            yield return Fixture(
                "Catch",
                "using System; class C { static void Root() { try { Target(); } catch (InvalidOperationException) { } } static void Target() { throw new InvalidOperationException(); } }");
            yield return Fixture(
                "UnresolvedCall",
                "using System; class C { static void Root(Action action) { action(); } }");
            yield return Fixture(
                "GenericCallable",
                "using System; class C<T> { static void Root<U>(U value) { if (value is null) throw new ArgumentNullException(); } }");
            yield return Fixture(
                "Overload",
                "using System; class C { static void Root() { Target(1); Target(\"x\"); } static void Target(int value) { throw new ArgumentException(); } static void Target(string value) { throw new InvalidOperationException(); } }");
            yield return Fixture(
                "ConstructorAccessor",
                "using System; class C { int Value { get { throw new InvalidOperationException(); } } C() { } static void Root() { C value = new C(); _ = value.Value; } }");
            yield return Fixture(
                "ContextSensitive",
                "using System; class C { static void Root() { Target(\"value\"); Target(null); } static void Target(string? value) { ArgumentNullException.ThrowIfNull(value); } }");
        }

        /// <summary>
        /// Preserves complete summary semantics through canonical IR and exact rebinding.
        /// </summary>
        [Theory]
        [MemberData(nameof(EquivalenceFixtures))]
        public void Summary_ExistingCanonicalExisting_PreservesSemanticIdentity(
            string fixtureName,
            string source)
        {
            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(source, "Root");

            Assert.True(
                RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(
                    run.RootKey,
                    run.RootSummary,
                    out CanonicalExceptionFlowSummary? canonical));
            Assert.NotNull(canonical);

            RoslynCanonicalIdentityResolver resolver = new(run.Compilation);
            Assert.NotNull(canonical.Callable.ContainingType);
            Assert.NotNull(resolver.ResolveType(canonical.Callable.ContainingType!));
            Assert.NotNull(resolver.ResolveCallableSymbol(canonical.Callable));

            if (canonical.CallContext.Callable != null)
            {
                Assert.NotNull(resolver.ResolveCallableSymbol(canonical.CallContext.Callable));
            }

            foreach (CanonicalExceptionFlowSummarySource summarySource in canonical.Sources)
            {
                Assert.NotNull(resolver.ResolveType(summarySource.ExceptionType));
            }

            foreach (CanonicalExceptionFlowCallEdge callEdge in canonical.CallEdges)
            {
                Assert.NotNull(resolver.ResolveCallableSymbol(callEdge.Target));
            }

            Assert.True(
                RoslynCanonicalExceptionFlowAdapter.TryResolveSummary(
                    canonical,
                    resolver,
                    out RoslynExceptionFlowSummaryResolution? resolved),
                fixtureName);
            Assert.NotNull(resolved);
            Assert.True(
                RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(
                    resolved.Key,
                    resolved.Summary,
                    out CanonicalExceptionFlowSummary? roundtrip));
            Assert.Equal(canonical, roundtrip);
        }

        /// <summary>
        /// Preserves assembly, source, context, edge, and summary models in JSON.
        /// </summary>
        [Fact]
        public void CanonicalModels_JsonRoundtrip_PreserveEquality()
        {
            const string source =
                "using System; class C { static void Root() { Target(); } static void Target() { throw new InvalidOperationException(); } }";
            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(source, "Root");
            Assert.True(
                RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(
                    run.RootKey,
                    run.RootSummary,
                    out CanonicalExceptionFlowSummary? summary));
            Assert.NotNull(summary);

            CanonicalAssemblyIdentity assembly =
                RoslynCanonicalIdentityFactory.CreateAssemblyIdentity(run.Compilation.Assembly);
            CanonicalExceptionFlowCallEdge edge = Assert.Single(summary.CallEdges);
            ExceptionFlowSummaryCallEdge existingEdge = Assert.Single(run.RootSummary.CallEdges);
            CanonicalExceptionFlowSummarySource sourceModel = Assert.Single(
                RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(
                    existingEdge.Target,
                    run.GetRequiredSummary(existingEdge.Target),
                    out CanonicalExceptionFlowSummary? targetSummary)
                    ? targetSummary!.Sources
                    : Array.Empty<CanonicalExceptionFlowSummarySource>());

            AssertJsonRoundtrip(assembly);
            AssertJsonRoundtrip(summary.Callable.ContainingType);
            AssertJsonRoundtrip(summary.CallContext);
            AssertJsonRoundtrip(edge);
            AssertJsonRoundtrip(sourceModel);
            AssertJsonRoundtrip(summary);
        }

        /// <summary>
        /// Resolves exact and base catches through Roslyn's authoritative hierarchy.
        /// </summary>
        [Theory]
        [InlineData("ArgumentNullException", true)]
        [InlineData("ArgumentException", true)]
        [InlineData("InvalidOperationException", false)]
        public void CanonicalCatch_ReboundEdge_PreservesTypeHierarchy(
            string caughtTypeName,
            bool expectedSuppression)
        {
            string source =
                "using System; class C { static void Root() { try { Target(); } catch (" +
                caughtTypeName +
                ") { } } static void Target() { throw new ArgumentNullException(); } }";
            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(source, "Root");
            ExceptionFlowSummaryCallEdge edge = Assert.Single(run.RootSummary.CallEdges);

            Assert.Equal(
                expectedSuppression,
                edge.Suppresses(run.GetRequiredType("System.ArgumentNullException")));
            Assert.True(
                RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(
                    run.RootKey,
                    run.RootSummary,
                    out CanonicalExceptionFlowSummary? canonical));
            Assert.True(
                RoslynCanonicalExceptionFlowAdapter.TryResolveSummary(
                    canonical!,
                    new RoslynCanonicalIdentityResolver(run.Compilation),
                    out RoslynExceptionFlowSummaryResolution? resolved));
            Assert.Equal(
                expectedSuppression,
                Assert.Single(resolved!.Summary.CallEdges)
                    .Suppresses(run.GetRequiredType("System.ArgumentNullException")));
        }

        /// <summary>
        /// Retains no escaping call edge after an unconditional catch-all.
        /// </summary>
        [Theory]
        [InlineData("catch")]
        [InlineData("catch (Exception)")]
        public void CanonicalCatch_CatchAll_RemainsSuppressed(string catchClause)
        {
            string source =
                "using System; class C { static void Root() { try { Target(); } " +
                catchClause +
                " { } } static void Target() { throw new InvalidOperationException(); } }";
            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(source, "Root");

            Assert.Empty(run.RootSummary.CallEdges);
            Assert.True(
                RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(
                    run.RootKey,
                    run.RootSummary,
                    out CanonicalExceptionFlowSummary? canonical));
            Assert.Empty(canonical!.CallEdges);
        }

        /// <summary>
        /// Keeps filtered catches conservative instead of recording suppression.
        /// </summary>
        [Fact]
        public void CanonicalCatch_FilteredCatch_RemainsConservative()
        {
            const string source =
                "using System; class C { static bool Filter() => true; static void Root() { try { Target(); } catch (Exception) when (Filter()) { } } static void Target() { throw new InvalidOperationException(); } }";
            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(source, "Root");
            ExceptionFlowSummaryCallEdge targetEdge = run.RootSummary.CallEdges
                .Single(edge => edge.Target.Symbol.Name == "Target");

            Assert.Empty(targetEdge.CaughtExceptionTypes);
            Assert.True(
                RoslynCanonicalExceptionFlowAdapter.TryCreateSummary(
                    run.RootKey,
                    run.RootSummary,
                    out CanonicalExceptionFlowSummary? canonical));
            CanonicalExceptionFlowCallEdge canonicalTarget = canonical!.CallEdges
                .Single(edge => edge.Target.MetadataName == "Target");
            Assert.Equal(CanonicalExceptionFlowCatchKind.None, canonicalTarget.CatchBehavior.Kind);
        }

        /// <summary>
        /// Distinguishes caught types with the same metadata name in different assemblies.
        /// </summary>
        [Fact]
        public void CanonicalCatch_SameNameDifferentAssembly_NotEqual()
        {
            CSharpCompilation first = CreateCompilation(
                "FirstCatchAssembly",
                "namespace Shared { public class SameException : System.Exception { } }");
            CSharpCompilation second = CreateCompilation(
                "SecondCatchAssembly",
                "namespace Shared { public class SameException : System.Exception { } }");
            CanonicalTypeIdentity firstType = RoslynCanonicalIdentityFactory.CreateTypeIdentity(
                first.GetTypeByMetadataName("Shared.SameException")!);
            CanonicalTypeIdentity secondType = RoslynCanonicalIdentityFactory.CreateTypeIdentity(
                second.GetTypeByMetadataName("Shared.SameException")!);

            Assert.NotEqual(firstType, secondType);
            Assert.NotEqual(
                new CanonicalExceptionFlowCatch(
                    CanonicalExceptionFlowCatchKind.Typed,
                    hasFilter: false,
                    new[] { firstType }),
                new CanonicalExceptionFlowCatch(
                    CanonicalExceptionFlowCatchKind.Typed,
                    hasFilter: false,
                    new[] { secondType }));
        }

        /// <summary>
        /// Preserves distinct context value and stable-member facts across compilations.
        /// </summary>
        [Fact]
        public void CallContext_CrossCompilationFacts_PreserveValueSemantics()
        {
            const string source =
                "class C { string Name { get; } = string.Empty; static void Target(C value) { } }";
            CSharpCompilation first = CreateCompilation("Contexts", source);
            CSharpCompilation second = CreateCompilation("Contexts", source);
            CanonicalExceptionFlowCallContext firstContext = CreateContext(first);
            CanonicalExceptionFlowCallContext secondContext = CreateContext(second);
            CanonicalExceptionFlowCallContext differentContext = new(
                firstContext.Callable,
                new[] { new CanonicalParameterValueFact(0, ExceptionFlowValueFacts.PositiveInt32) },
                firstContext.MemberFacts);

            Assert.Equal(firstContext, secondContext);
            Assert.NotEqual(firstContext, differentContext);
        }

        /// <summary>
        /// Keeps hash semantics compatible while legacy string-key callers remain.
        /// </summary>
        [Fact]
        public void CallableKey_CanonicalAndLegacyEquivalent_HaveEqualHashes()
        {
            CSharpCompilation compilation = CreateCompilation(
                "MixedKeys",
                "class C { static void Target(string value) { } }");
            INamedTypeSymbol type = compilation.GetTypeByMetadataName("C")!;
            IMethodSymbol method = type.GetMembers("Target").OfType<IMethodSymbol>().Single();
            ExceptionFlowCallContext context = new(
                method,
                new[]
                {
                    new KeyValuePair<int, ExceptionFlowValueFacts>(
                        0,
                        ExceptionFlowValueFacts.NonNull)
                });
            ExceptionFlowCallableKey canonicalKey = new(method, context);
            ExceptionFlowCallableKey legacyKey = new(method, context.Key);

            Assert.Equal(canonicalKey, legacyKey);
            Assert.Equal(canonicalKey.GetHashCode(), legacyKey.GetHashCode());
        }

        /// <summary>
        /// Preserves canonical result paths, evidence, and uncertainty on exact rebinding.
        /// </summary>
        [Fact]
        public void AnalysisResult_ExistingCanonicalExisting_PreservesSemanticIdentity()
        {
            const string source =
                "using System; class C { static void Root() { throw new InvalidOperationException(); } }";
            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeDirectly(source, "Root");
            run.Result.UncertainTargets.Add("Unresolved.Target()");
            CanonicalExceptionFlowAnalysisResult canonical =
                RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(run.Result);

            AssertJsonRoundtrip(canonical);
            Assert.True(
                RoslynCanonicalExceptionFlowAdapter.TryResolveAnalysisResult(
                    canonical,
                    new RoslynCanonicalIdentityResolver(run.Compilation),
                    out XMLDocNormalizer.Models.DTO.ExceptionFlowAnalysisResult? resolved));
            Assert.NotNull(resolved);
            Assert.Equal(
                canonical,
                RoslynCanonicalExceptionFlowAdapter.CreateAnalysisResult(resolved));
        }

        private static object[] Fixture(string name, string source)
        {
            return new object[] { name, source };
        }

        private static void AssertJsonRoundtrip<T>(T value)
        {
            string firstJson = JsonSerializer.Serialize(value);
            T? roundtrip = JsonSerializer.Deserialize<T>(firstJson);
            string secondJson = JsonSerializer.Serialize(roundtrip);

            Assert.Equal(value, roundtrip);
            Assert.Equal(firstJson, secondJson);
        }

        private static CSharpCompilation CreateCompilation(string assemblyName, string source)
        {
            return CSharpCompilation.Create(
                assemblyName,
                new[] { CSharpSyntaxTree.ParseText(source, path: "CanonicalFlowFixture.cs") },
                MetadataReferences.Default,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        private static CanonicalExceptionFlowCallContext CreateContext(CSharpCompilation compilation)
        {
            INamedTypeSymbol type = compilation.GetTypeByMetadataName("C")!;
            IMethodSymbol method = type.GetMembers("Target").OfType<IMethodSymbol>().Single();
            ISymbol member = type.GetMembers("Name").Single();
            ExceptionFlowCallContext context = new(
                method,
                new[]
                {
                    new KeyValuePair<int, ExceptionFlowValueFacts>(
                        0,
                        ExceptionFlowValueFacts.NonNull)
                },
                new[] { new KeyValuePair<int, ISymbol>(0, member) });

            return Assert.IsType<CanonicalExceptionFlowCallContext>(
                context.CanonicalContext);
        }
    }
}
