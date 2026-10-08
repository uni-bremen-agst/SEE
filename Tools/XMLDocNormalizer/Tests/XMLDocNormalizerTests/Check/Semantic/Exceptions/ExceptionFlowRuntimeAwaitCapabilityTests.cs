using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure.Exception;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;
using XMLDocNormalizerTests.Helpers;
using Information = XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowRuntimeAwaitCapability.Information;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>Tests the three-valued runtime-await boundary and its final flow decisions.</summary>
    public sealed class ExceptionFlowRuntimeAwaitCapabilityTests
    {
        private const string Source = """
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading.Tasks;
            public static class Entry
            {
                /// <exception cref="InvalidOperationException">A possible exception.</exception>
                public static async Task M(Awaitable value) { await value; }
                public static void Caller() { }
                public static void RuntimeHelper() { throw new InvalidOperationException(); }
            }
            public readonly struct Awaitable
            {
                public Awaiter GetAwaiter() => default;
            }
            public readonly struct Awaiter : INotifyCompletion
            {
                public bool IsCompleted => true;
                public void GetResult() { }
                public void OnCompleted(Action continuation) { }
            }
            """;

        /// <summary>Distinguishes unavailable information from a known native null.</summary>
        [Fact]
        public void DefaultInformation_IsUnavailable_NotKnownNull()
        {
            Information unavailable = default;
            Information knownNull = Information.Known(null);
            Assert.False(unavailable.IsAvailable);
            Assert.True(knownNull.IsAvailable);
            Assert.Null(unavailable.Method);
            Assert.Null(knownNull.Method);
        }

        /// <summary>Matches the actual current getter for normal, dynamic and incomplete awaits.</summary>
        /// <param name="parameter">The fixture parameter declaration.</param>
        /// <param name="expression">The awaited expression.</param>
        [Theory]
        [InlineData("Task value", "value")]
        [InlineData("Task<int> value", "value")]
        [InlineData("ValueTask value", "value")]
        [InlineData("ValueTask<int> value", "value")]
        [InlineData("Awaitable value", "value")]
        [InlineData("ExtensionAwaitable value", "value")]
        [InlineData("dynamic value", "value")]
        [InlineData("int value", "value")]
        public void CurrentNativeGetter_ReturnsIdenticalValue(string parameter, string expression)
        {
            string source = Source.Replace("Awaitable value", parameter)
                .Replace("await value;", "await " + expression + ";") + """

                public readonly struct ExtensionAwaitable { }
                public static class Extensions
                {
                    public static Awaiter GetAwaiter(this ExtensionAwaitable value) => default;
                }
                """;
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
            CSharpCompilation compilation = CSharpCompilation.Create("NativeAwait",
                [tree], MetadataReferences.Default,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            Diagnostic[] errors = compilation.GetDiagnostics()
                .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
            if (parameter == "int value")
            {
                Assert.NotEmpty(errors);
            }
            else
            {
                Assert.Empty(errors);
            }

            AwaitExpressionSyntax syntax = Assert.Single(tree.GetRoot().DescendantNodes().OfType<AwaitExpressionSyntax>());
            AwaitExpressionInfo info = compilation.GetSemanticModel(tree).GetAwaitExpressionInfo(syntax);
            Information result = ExceptionFlowRuntimeAwaitCapability.Read(info);
            Assert.True(result.IsAvailable);
            Assert.Same(info.RuntimeAwaitMethod, result.Method);
        }

        /// <summary>Preserves native null for the default compiler snapshot.</summary>
        [Fact]
        public void DefaultSnapshot_CurrentGetter_IsKnownNull()
        {
            Information result = ExceptionFlowRuntimeAwaitCapability.Read(default);
            Assert.True(result.IsAvailable);
            Assert.Null(result.Method);
        }

        /// <summary>
        /// Exercises real current getter storage with non-null symbols, not historical binding.
        /// </summary>
        /// <param name="completePattern">Whether normal pattern fields also exist.</param>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void CurrentStructuralNonNullGetter_PreservesIdentity(bool completePattern)
        {
            var fixture = CreateFixture();
            IMethodSymbol helper = fixture.Run.Compilation.GetTypeByMetadataName("Entry")!
                .GetMembers("RuntimeHelper").OfType<IMethodSymbol>().Single();
            ConstructorInfo constructor = typeof(AwaitExpressionInfo)
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(candidate => candidate.GetParameters().Length == 5);
            AwaitExpressionInfo info = (AwaitExpressionInfo)constructor.Invoke(
                [completePattern ? fixture.Info.GetAwaiterMethod : null,
                 completePattern ? fixture.Info.IsCompletedProperty : null,
                 completePattern ? fixture.Info.GetResultMethod : null, helper, false]);
            Assert.Same(helper, info.RuntimeAwaitMethod);
            Parallel.For(0, 1000, _ =>
            {
                Information result = ExceptionFlowRuntimeAwaitCapability.Read(info);
                Assert.True(result.IsAvailable);
                Assert.Same(info.RuntimeAwaitMethod, result.Method);
            });
        }

        /// <summary>
        /// Runs both actual call-site bodies through fragment, summary, transitive evaluation
        /// and DOC631/DOC632 decisions for known-null, symbol and unavailable states.
        /// </summary>
        /// <param name="dispatch">Which of the two productive bodies is exercised.</param>
        /// <param name="state">The capability state.</param>
        [Theory]
        [InlineData(false, "unavailable")]
        [InlineData(true, "unavailable")]
        [InlineData(false, "null")]
        [InlineData(true, "null")]
        [InlineData(false, "symbol")]
        [InlineData(true, "symbol")]
        public void CallSite_ThreeStates_ReachConservativeFinalFinding(bool dispatch, string state)
        {
            var fixture = CreateFixture();
            Assert.NotNull(fixture.Info.GetAwaiterMethod);
            Assert.NotNull(fixture.Info.IsCompletedProperty?.GetMethod);
            Assert.NotNull(fixture.Info.GetResultMethod);
            IMethodSymbol helper = fixture.Run.Compilation.GetTypeByMetadataName("Entry")!
                .GetMembers("RuntimeHelper").OfType<IMethodSymbol>().Single();
            Information information = state switch
            {
                "null" => Information.Known(null),
                "symbol" => Information.Known(helper),
                _ => default
            };
            ExceptionFlowSummaryGraph graph = new();
            ExceptionFlowSummaryFragment fragment = InvokeCallSite(fixture, dispatch, information, graph);
            Assert.Equal(state == "unavailable" ? 0 : state == "symbol" ? 1 : 3, fragment.CallEdges.Count);
            Assert.Equal(state == "unavailable" ? 1 : 0, fragment.UncertainTargets.Count);
            if (state == "symbol")
            {
                ExceptionFlowSummaryCallEdge edge = Assert.Single(fragment.CallEdges);
                Assert.Equal(ExceptionFlowPathStepKind.RuntimeAwaitCall, edge.CallSiteStep.Kind);
                Assert.True(SymbolEqualityComparer.Default.Equals(helper, edge.Target.Symbol));
            }

            // Use actual source-built target summaries, not guessed target exception sets.
            var helperRun = ExceptionFlowSummaryGraphTestHelper.Build(Source, "RuntimeHelper");
            foreach (ExceptionFlowSummaryCallEdge edge in fragment.CallEdges)
            {
                ExceptionFlowSummary actual = state == "symbol"
                    ? helperRun.RootSummary : fixture.Run.GetRequiredSummary(edge.Target);
                CopySummary(actual, graph.GetOrAdd(edge.Target));
            }
            ExceptionFlowAnalysisResult result = EvaluateThroughCaller(fixture, graph, fragment);
            Assert.Equal(state == "unavailable", result.HasUncertainPaths);
            List<Finding> findings = GetFinalFindings(fixture, result);
            Assert.Equal(state == "unavailable" ? "DOC631" : state == "null" ? "DOC632" : null,
                findings.SingleOrDefault()?.Smell.ID);
            if (state == "symbol")
            {
                INamedTypeSymbol exception = fixture.Run.GetRequiredType("System.InvalidOperationException");
                ExceptionFlowPath path = Assert.Single(result.GetExceptionPaths(exception));
                Assert.Contains(path.Steps, step => step.Kind == ExceptionFlowPathStepKind.RuntimeAwaitCall);
            }
        }

        /// <summary>Typed catches preserve unknown flow; a proven catch-all may clear it.</summary>
        /// <param name="dispatch">Which call-site body to use.</param>
        /// <param name="catchAll">Whether all possible exceptions are caught.</param>
        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public void UnavailableInformation_CatchTransfer_RemainsConservative(bool dispatch, bool catchAll)
        {
            var fixture = CreateFixture();
            ExceptionFlowSummaryGraph graph = new();
            ExceptionFlowSummaryFragment fragment = InvokeCallSite(fixture, dispatch, default, graph);
            if (catchAll)
            {
                fragment.SuppressAll();
            }
            else
            {
                fragment.SuppressCaughtException(fixture.Run.GetRequiredType("System.InvalidOperationException"));
            }
            ExceptionFlowAnalysisResult result = EvaluateThroughCaller(fixture, graph, fragment);
            Assert.Equal(!catchAll, result.HasUncertainPaths);
            Assert.Equal(catchAll ? "DOC632" : "DOC631", Assert.Single(GetFinalFindings(fixture, result)).Smell.ID);
        }

        /// <summary>Uncertainty does not delete independently proven exception sources.</summary>
        /// <param name="dispatch">Which call-site body to use.</param>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void UnavailableInformation_PreservesOtherProvenSources(bool dispatch)
        {
            var fixture = CreateFixture();
            ExceptionFlowSummaryGraph graph = new();
            ExceptionFlowSummaryFragment fragment = InvokeCallSite(fixture, dispatch, default, graph);
            var proven = ExceptionFlowSummaryGraphTestHelper.Build(Source, "RuntimeHelper");
            fragment.AddSource(Assert.Single(proven.RootSummary.Sources));
            ExceptionFlowAnalysisResult result = EvaluateThroughCaller(fixture, graph, fragment);
            Assert.True(result.HasUncertainPaths);
            Assert.Single(result.ThrownExceptions);
            Assert.Empty(GetFinalFindings(fixture, result));
        }

        /// <summary>Creates a real complete normal awaiter binding and its source-built graph.</summary>
        private static Fixture CreateFixture()
        {
            var run = ExceptionFlowSummaryGraphTestHelper.Build(Source, "M");
            SyntaxTree tree = run.Compilation.SyntaxTrees.Single();
            SemanticModel model = run.Compilation.GetSemanticModel(tree);
            AwaitExpressionSyntax syntax = tree.GetRoot().DescendantNodes().OfType<AwaitExpressionSyntax>().Single();
            var context = ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, run.Compilation);
            return new Fixture(run, model, syntax, model.GetAwaitExpressionInfo(syntax), context);
        }

        /// <summary>Supplies immutable per-call information to the real private algorithm body.</summary>
        private static ExceptionFlowSummaryFragment InvokeCallSite(Fixture fixture, bool dispatch,
            Information information, ExceptionFlowSummaryGraph graph)
        {
            ExceptionFlowSummaryFragment fragment = new();
            typeof(ExceptionFlowAnalyzer).GetMethod(dispatch
                ? "AddSummaryExplicitAwaitDispatchEdges" : "AddSummaryExplicitAwaitEdges",
                BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null,
                [fixture.Info, information, fixture.Syntax, fixture.Syntax.Expression, "Await expression",
                 fixture.Model, new ExceptionFlowSemanticEnvironment(fixture.Context), graph, fragment,
                 new ExceptionFlowCallContext(fixture.Run.RootKey.Symbol)]);
            return fragment;
        }

        /// <summary>Copies a real target summary without transferring ownership of its data.</summary>
        private static void CopySummary(ExceptionFlowSummary source, ExceptionFlowSummary target)
        {
            ExceptionFlowSummaryFragment copy = new();
            foreach (var entry in source.Sources) copy.AddSource(entry);
            foreach (var edge in source.CallEdges) copy.AddCallEdge(edge);
            foreach (var uncertainty in source.UncertainTargets) copy.AddUncertainTarget(uncertainty);
            target.MarkExecutableBodyAnalyzed();
            target.Merge(copy);
        }

        /// <summary>Exercises both local-summary entry and transitive caller-result merge.</summary>
        private static ExceptionFlowAnalysisResult EvaluateThroughCaller(Fixture fixture,
            ExceptionFlowSummaryGraph graph, ExceptionFlowSummaryFragment fragment)
        {
            var summary = graph.GetOrAdd(fixture.Run.RootKey);
            summary.MarkExecutableBodyAnalyzed();
            summary.Merge(fragment);
            IMethodSymbol caller = fixture.Run.Compilation.GetTypeByMetadataName("Entry")!
                .GetMembers("Caller").OfType<IMethodSymbol>().Single();
            var callerKey = new ExceptionFlowCallableKey(caller, new ExceptionFlowCallContext(caller));
            ExceptionFlowSummaryFragment callerFragment = new();
            callerFragment.AddCallEdge(new ExceptionFlowSummaryCallEdge(fixture.Run.RootKey,
                ExceptionFlowPathFactory.CreateStep(ExceptionFlowPathStepKind.MethodCall, caller, fixture.Syntax)));
            var callerSummary = graph.GetOrAdd(callerKey);
            callerSummary.MarkExecutableBodyAnalyzed();
            callerSummary.Merge(callerFragment);
            return new ExceptionFlowSummaryGraphEvaluator().Evaluate(graph, callerKey, fixture.Run.Compilation);
        }

        /// <summary>Runs the actual detector's final DOC631 and DOC632 decisions on the evaluated result.</summary>
        private static List<Finding> GetFinalFindings(Fixture fixture, ExceptionFlowAnalysisResult result)
        {
            XmlElementSyntax element = fixture.Syntax.SyntaxTree.GetRoot()
                .DescendantNodes(descendIntoTrivia: true).OfType<XmlElementSyntax>().Single();
            var cref = element.StartTag.Attributes.OfType<XmlCrefAttributeSyntax>().Single();
            List<ExceptionTagSemanticInfo> tags = [new()
            {
                Tag = new ExtractedXmlDocTag(element, "InvalidOperationException"),
                CrefAttribute = cref,
                ResolvedSymbol = fixture.Model.GetSymbolInfo(cref.Cref).Symbol,
                FindingContext = FindingContext.Unknown
            }];
            Assert.NotNull(tags[0].ResolvedTypeSymbol);
            List<Finding> findings = new();
            foreach (string name in new[] { "AddExceptionFlowNotDecidableFindings", "AddDocumentedExceptionWithoutTransitiveThrowFindings" })
            {
                typeof(XmlDocExceptionSemanticDetector).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!
                    .Invoke(null, [findings, fixture.Syntax.SyntaxTree, fixture.Syntax.SyntaxTree.FilePath, tags,
                        fixture.Run.GetRequiredType("System.Exception"), result,
                        new XmlDocOptions { ExceptionAnalysisMode = ExceptionAnalysisMode.SolutionTransitive }, fixture.Context]);
            }
            return findings;
        }

        /// <summary>Holds only test-local current Roslyn inputs; no global capability hooks exist.</summary>
        private sealed record Fixture(ExceptionFlowSummaryGraphTestRun Run, SemanticModel Model,
            AwaitExpressionSyntax Syntax, AwaitExpressionInfo Info, ProjectClosureSemanticContext Context);
    }
}
