using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;
using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowFiniteOrbitProfile;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Observes existing Roslyn/admission contracts; implements no proposed store, graph or replayer.</summary>
public sealed class ExceptionFlowSharedSchemaFactsContractTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedGenericSchemaShapeAndRawBindingStayFixedWhileConcreteSourcesDiffer(bool reverse)
    {
        var fixture = new AdmissionCharacterization.Fixture([("/P5B/Nested.cs", """
            class Fault<T,U,V> : System.Exception {}
            class Outer<T> { public class Inner<U> {
                public static void Run<V>() { Leaf(); throw new Fault<T,U,V>(); }
                static void Leaf() {}
            } }
            """)]);
        var run = fixture.Session();
        var integer = fixture.Compilation.GetSpecialType(SpecialType.System_Int32);
        var text = fixture.Compilation.GetSpecialType(SpecialType.System_String);
        var outer = fixture.Compilation.GetTypeByMetadataName("Outer`1")!;
        IMethodSymbol Construct(ITypeSymbol first, ITypeSymbol second) => outer.Construct(first)
            .GetTypeMembers("Inner").Single().Construct(second).GetMembers("Run").OfType<IMethodSymbol>().Single().Construct(first);
        IMethodSymbol[] contexts = [Construct(integer, text), Construct(text, integer)];
        if (reverse) Array.Reverse(contexts);
        var definition = contexts[0].OriginalDefinition;
        string shape = SchemaShape(definition);
        var syntax = (MethodDeclarationSyntax)definition.DeclaringSyntaxReferences.Single().GetSyntax();
        Assert.True(run.Environment.TryGetSemanticModel(syntax.SyntaxTree, out var model));
        var invocation = syntax.DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
        var selected = model.GetSymbolInfo(invocation).Symbol!;
        var operation = model.GetOperation(syntax.Body!);
        Assert.NotNull(operation);
        var sources = new List<string>();
        var traces = new List<string>();
        foreach (var context in contexts.Append(contexts[0]))
        {
            Assert.True(SymbolEqualityComparer.Default.Equals(definition, context.OriginalDefinition));
            Assert.Equal(shape, SchemaShape(context));
            Assert.True(SymbolEqualityComparer.Default.Equals(selected, model.GetSymbolInfo(invocation).Symbol));
            Assert.Equal(operation.Kind, model.GetOperation(syntax.Body!)!.Kind);
            var concrete = Local(definition, run.Environment, new(context));
            Assert.True(concrete.HasBody);
            sources.Add(AdmissionCharacterization.TypeKey(Assert.Single(concrete.Fragment.Sources).ExceptionType));
            var schema = Local(definition, run.Environment, new(definition));
            traces.Add(string.Join("\n", schema.Sink.Events) + AdmissionCharacterization.Fragment(schema.Fragment));
        }
        Assert.NotEqual(sources[0], sources[1]);
        Assert.Equal(sources[0], sources[2]);
        Assert.Equal(traces[0], traces[1]);
        Assert.Equal(traces[0], traces[2]);
        Assert.NotEqual(new ExceptionFlowCallableKey(definition, new ExceptionFlowCallContext(contexts[0])),
            new ExceptionFlowCallableKey(definition, new ExceptionFlowCallContext(contexts[1])));
    }

    [Fact]
    public void CallerNonNullFactsChangeLocalFlowWithoutChangingDeclarationOrRawBinding()
    {
        var fixture = new AdmissionCharacterization.Fixture([("/P5B/Values.cs", """
            class Entry {
                static void Root() { Check("known"); Check(null); }
                static void Check(string? value) { if (value == null) throw new System.ArgumentNullException(); }
            }
            """)]);
        var run = fixture.Session();
        var calls = fixture.Root("Root").DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
        var model = fixture.Compilation.GetSemanticModel(calls[0].SyntaxTree);
        var definition = fixture.Symbol("Check");
        var contexts = calls.Select(call => ExceptionFlowAnalyzer.CreateInvocationCallContext(call,
            (IMethodSymbol)model.GetSymbolInfo(call).Symbol!, model, new(fixture.Symbol("Root")))).ToArray();
        Assert.True(contexts[0].GetParameterFacts(0).ContainsAll(ExceptionFlowValueFacts.NonNull));
        Assert.Equal(ExceptionFlowValueFacts.None, contexts[1].GetParameterFacts(0));
        Assert.Equal(SchemaShape(contexts[0].InvocationSymbol!), SchemaShape(contexts[1].InvocationSymbol!));
        Assert.Empty(Local(definition, run.Environment, contexts[0]).Fragment.Sources);
        Assert.Equal("ArgumentNullException", Assert.Single(Local(definition, run.Environment, contexts[1]).Fragment.Sources).ExceptionType.Name);
        Assert.NotEqual(new ExceptionFlowCallableKey(definition, contexts[0]), new ExceptionFlowCallableKey(definition, contexts[1]));
    }

    [Fact]
    public void ReducedExtensionKeepsReceiverFactsSeparateFromUnreducedSchemaShape()
    {
        var fixture = new AdmissionCharacterization.Fixture([("/P5B/Extension.cs", """
            static class Extensions {
                public static void Check<T>(this T value) where T : class {
                    if (value == null) throw new System.ArgumentNullException();
                }
            }
            class Entry { static void Root() { "known".Check(); ((string)null!).Check(); } }
            """)]);
        var run = fixture.Session();
        var calls = fixture.Root("Root").DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
        var model = fixture.Compilation.GetSemanticModel(calls[0].SyntaxTree);
        var selected = calls.Select(call => (IMethodSymbol)model.GetSymbolInfo(call).Symbol!).ToArray();
        Assert.All(selected, method => Assert.NotNull(method.ReducedFrom));
        var targets = selected.Select(ExceptionFlowAnalyzer.GetInvocationAnalysisTarget).ToArray();
        Assert.Equal(SchemaShape(targets[0]), SchemaShape(targets[1]));
        Assert.All(targets, target => Assert.Equal(SpecialType.System_String, target.TypeArguments.Single().SpecialType));
        var contexts = calls.Select((call, index) => ExceptionFlowAnalyzer.CreateInvocationCallContext(
            call, selected[index], model, new(fixture.Symbol("Root")))).ToArray();
        Assert.True(contexts[0].GetParameterFacts(0).ContainsAll(ExceptionFlowValueFacts.NonNull));
        Assert.Equal(ExceptionFlowValueFacts.None, contexts[1].GetParameterFacts(0));
        Assert.Empty(Local(targets[0].OriginalDefinition, run.Environment, contexts[0]).Fragment.Sources);
        Assert.Single(Local(targets[1].OriginalDefinition, run.Environment, contexts[1]).Fragment.Sources);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RealP2CReplaySharesOnlyCompletedTapeAndCreatesFreshRootConsumers(bool reverse)
    {
        var fixture = new AdmissionCharacterization.Fixture([
            Root("A", "try { Shared<int>(); } catch {}"), Root("B", "Shared<int>();"),
            ("/P5B/Shared.cs", """
            public static partial class Entry {
                static void Shared<T>() { Leaf<T>(); Leaf<T>(); }
                static void Leaf<T>() { throw new Fault<T>(); }
            }
            class Fault<T> : System.Exception {}
            """)
        ]);
        var run = fixture.Session();
        int cold = 0;
        var facts = new ExceptionFlowFiniteOrbitAdmission.FactStore(run.Environment, symbol =>
        {
            if (symbol.Name == "Shared" && ++cold > 1) throw new InvalidOperationException("Unexpected second producer");
        });
        string[] order = reverse ? ["B", "A"] : ["A", "B"];
        var first = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root(order[0]), run.Environment, facts);
        var second = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root(order[1]), run.Environment, facts);
        Assert.Equal(1, cold);
        Assert.True(first.Outcome.Complete);
        Assert.True(second.Outcome.Complete);
        Assert.NotEqual(first.Outcome.Root, second.Outcome.Root);
        Assert.NotEqual(first.Outcome.InventoryDigest, second.Outcome.InventoryDigest);
        string shared = ExceptionFlowFiniteOrbitAdmission.BodyId(fixture.Symbol("Shared"));
        var firstLocal = Diagnostics(first)[shared];
        var secondLocal = Diagnostics(second)[shared];
        Assert.Equal(AdmissionCharacterization.Fragment(firstLocal.Fragment), AdmissionCharacterization.Fragment(secondLocal.Fragment));
        Assert.NotSame(firstLocal.Fragment, secondLocal.Fragment);
        Assert.Equal(2, firstLocal.Fragment.CallEdges.Count);
        Assert.Equal(2, secondLocal.Fragment.CallEdges.Count);
        for (int index = 0; index < 2; index++)
        {
            Assert.Equal(firstLocal.Fragment.CallEdges[index].Target, secondLocal.Fragment.CallEdges[index].Target);
            Assert.NotSame(firstLocal.Fragment.CallEdges[index].Target, secondLocal.Fragment.CallEdges[index].Target);
            Assert.NotSame(firstLocal.Fragment.CallEdges[index], secondLocal.Fragment.CallEdges[index]);
        }
        var firstEdges = RawEdges(first).Where(edge => edge.From == shared).ToArray();
        var secondEdges = RawEdges(second).Where(edge => edge.From == shared).ToArray();
        Assert.Equal(2, firstEdges.Length);
        Assert.Equal(firstEdges.Select(edge => edge.Witness), secondEdges.Select(edge => edge.Witness));
        Assert.NotSame(firstEdges[0].Assignments, firstEdges[1].Assignments);
        Assert.NotSame(firstEdges[0].Assignments, secondEdges[0].Assignments);
        Assert.All(secondEdges, edge => Assert.NotEmpty(edge.Assignments));
        var baselineA = fixture.Session(); var baselineB = fixture.Session();
        var warm = fixture.Session();
        foreach (string name in order)
            Assert.Equal(fixture.Capture(name, name == "A" ? baselineA.Context : baselineB.Context,
                    name == "A" ? baselineA.Session : baselineB.Session), fixture.Capture(name, warm.Context, warm.Session));
        Assert.Empty(warm.Session.Analyze(fixture.Root("A")).ThrownExceptions);
        Assert.Equal("Fault", Assert.Single(warm.Session.Analyze(fixture.Root("B")).ThrownExceptions).Name);
    }

    [Fact]
    public void SupportingGrowthRetiresFactsEvenWhenSchemaOwnerAndShapeStayTheSame()
    {
        var fixture = new AdmissionCharacterization.Fixture([
            Root("A", "Shared();"), Root("B", "Shared();"),
            ("/P5B/Shared.cs", "public static partial class Entry { static void Shared() {} }")
        ]);
        var run = fixture.Session();
        int cold = 0;
        var session = new ExceptionFlowSummaryAnalysisSession(run.Environment, symbol => { if (symbol.Name == "Shared") cold++; });
        session.Analyze(fixture.Root("A"));
        var facts = AdmissionCharacterization.Field<ExceptionFlowFiniteOrbitAdmission.FactStore>(session, "admissionFacts");
        var beforeFact = Entries(facts)[fixture.Symbol("Shared")];
        Assert.NotNull(beforeFact);
        string beforeShape = SchemaShape(fixture.Symbol("Shared"));
        var beforeView = run.Environment.GetAnalysisScopes();
        var support = new AdmissionCharacterization.Fixture([("/P5B/Support.cs", "public class Other {}")], assemblyName: "P5B.Support");
        run.Context.RegisterSupportingSource(support.Compilation);
        Assert.NotSame(beforeView, run.Environment.GetAnalysisScopes());
        Assert.Equal(beforeShape, SchemaShape(fixture.Symbol("Shared")));
        session.Analyze(fixture.Root("B"));
        Assert.Equal(2, cold);
        Assert.NotSame(beforeFact, Entries(facts)[fixture.Symbol("Shared")]);
        var baseline = fixture.Session(); baseline.Context.RegisterSupportingSource(support.Compilation);
        Assert.Equal(fixture.Capture("B", baseline.Context, baseline.Session), fixture.Capture("B", run.Context, session));
    }

    [Theory]
    [InlineData("method")]
    [InlineData("constructor")]
    [InlineData("getter")]
    [InlineData("setter")]
    public void MetadataNoBodyIsLocalAndContextIndependentAfterExactBinding(string member)
    {
        var dependency = Dependency();
        var reference = Emit(dependency.Compilation);
        var fixture = new AdmissionCharacterization.Fixture([Root("A", "")], MetadataReferences.Default.Append(reference));
        var run = fixture.Session();
        var owner = (IAssemblySymbol)fixture.Compilation.GetAssemblyOrModuleSymbol(reference)!;
        var api = owner.GetTypeByMetadataName("External.Api`1")!;
        var methods = new[] { SpecialType.System_Int32, SpecialType.System_String }.Select(type =>
        {
            var constructed = api.Construct(fixture.Compilation.GetSpecialType(type));
            return member switch
            {
                "constructor" => constructed.InstanceConstructors.Single(),
                "getter" => constructed.GetMembers("Value").OfType<IPropertySymbol>().Single().GetMethod!,
                "setter" => constructed.GetMembers("Value").OfType<IPropertySymbol>().Single().SetMethod!,
                _ => constructed.GetMembers("Run").OfType<IMethodSymbol>().Single().Construct(fixture.Compilation.GetSpecialType(type))
            };
        }).ToArray();
        Assert.True(SymbolEqualityComparer.Default.Equals(methods[0].OriginalDefinition, methods[1].OriginalDefinition));
        foreach (var method in methods)
        {
            Assert.False(method.IsImplicitlyDeclared);
            Assert.Empty(method.DeclaringSyntaxReferences);
            Assert.All(method.Locations, location => Assert.True(location.IsInMetadata));
            using var reads = run.Environment.BeginAdmissionReads();
            var local = Local(method.OriginalDefinition, run.Environment, new(method));
            Assert.False(local.HasBody);
            Assert.Empty(local.Fragment.Sources);
            Assert.Empty(local.Fragment.CallEdges);
            Assert.Empty(local.Fragment.UncertainTargets);
            Assert.Empty(local.Sink.Events);
            Assert.Empty(reads.Models);
            Assert.True(reads.Reusable);
        }
        Assert.Equal(SchemaShape(methods[0]), SchemaShape(methods[1]));
        Assert.NotEqual(new ExceptionFlowCallableKey(methods[0], new ExceptionFlowCallContext(methods[0])),
            new ExceptionFlowCallableKey(methods[1], new ExceptionFlowCallContext(methods[1])));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MetadataAcrossRootsHasFreshReasonsMapsAndNoExistingP2CHit(bool reverse)
    {
        var reference = Emit(Dependency().Compilation);
        var fixture = new AdmissionCharacterization.Fixture([
            Root("A", "External.Api<int>.Run<string>();"), Root("B", "External.Api<string>.Run<int>();")
        ], MetadataReferences.Default.Append(reference));
        var run = fixture.Session();
        var visited = new List<ISymbol>();
        var facts = new ExceptionFlowFiniteOrbitAdmission.FactStore(run.Environment, visited.Add);
        string[] order = reverse ? ["B", "A"] : ["A", "B"];
        var first = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root(order[0]), run.Environment, facts);
        var second = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root(order[1]), run.Environment, facts);
        var metadata = visited.Where(symbol => symbol.Name == "Run").ToArray();
        Assert.Equal(2, metadata.Length);
        Assert.True(SymbolEqualityComparer.Default.Equals(metadata[0], metadata[1]));
        Assert.Empty(Entries(facts));
        string id = ExceptionFlowFiniteOrbitAdmission.BodyId(metadata[0]);
        Assert.False(first.Outcome.Complete); Assert.False(second.Outcome.Complete);
        Assert.NotEqual(first.Outcome.Root, second.Outcome.Root);
        Assert.Equal(first.Outcome.ScopeDigest, second.Outcome.ScopeDigest);
        Assert.NotEqual(first.Outcome.InventoryDigest, second.Outcome.InventoryDigest);
        Assert.Contains(first.Outcome.Reasons, reason => reason.Witness == id + " / executable body unavailable");
        Assert.Contains(second.Outcome.Reasons, reason => reason.Witness == id + " / executable body unavailable");
        Assert.NotSame(Diagnostics(first)[id].Fragment, Diagnostics(second)[id].Fragment);
        Assert.NotSame(RawEdges(first).Single().Assignments, RawEdges(second).Single().Assignments);
        Assert.NotEqual(RawEdges(first).Single().Assignments.Select(term => term.Key), RawEdges(second).Single().Assignments.Select(term => term.Key));
    }

    [Theory]
    [InlineData("clone")]
    [InlineData("independent")]
    [InlineData("other-owner")]
    [InlineData("other-pe")]
    public void EqualMetadataShapeDoesNotAuthorizeCrossCompilationOrOwnerSharing(string change)
    {
        var dependency = Dependency();
        var reference = Emit(dependency.Compilation);
        var first = new AdmissionCharacterization.Fixture([Root("A", "")], MetadataReferences.Default.Append(reference));
        var otherReference = change switch
        {
            "other-owner" => Emit(Dependency("P5B.OtherOwner").Compilation),
            "other-pe" => Emit(Dependency(extra: "public static void Added() {}").Compilation),
            _ => reference
        };
        var second = change == "clone" ? first.Compilation.Clone() : new AdmissionCharacterization.Fixture(
            [Root("A", "")], MetadataReferences.Default.Append(otherReference)).Compilation;
        Assert.NotSame(first.Compilation, second);
        var firstEnvironment = first.Session().Environment;
        var secondEnvironment = new XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowSemanticEnvironment(
            XMLDocNormalizer.Execution.Semantic.ProjectClosureSemanticContext.CreateSingleCompilationContext(second.SyntaxTrees.Single(), second));
        Assert.NotSame(firstEnvironment.GetAnalysisScopes(), secondEnvironment.GetAnalysisScopes());
        IMethodSymbol Method(Compilation compilation) => compilation.GetTypeByMetadataName("External.Api`1")!
            .GetMembers("Run").OfType<IMethodSymbol>().Single();
        Assert.False(Local(Method(first.Compilation), firstEnvironment, new(Method(first.Compilation))).HasBody);
        Assert.False(Local(Method(second), secondEnvironment, new(Method(second))).HasBody);
        if (change == "other-owner")
            Assert.NotEqual(ExceptionFlowFiniteOrbitAdmission.BodyId(Method(first.Compilation)), ExceptionFlowFiniteOrbitAdmission.BodyId(Method(second)));
        else
            Assert.Equal(ExceptionFlowFiniteOrbitAdmission.BodyId(Method(first.Compilation)), ExceptionFlowFiniteOrbitAdmission.BodyId(Method(second)));
        // Roslyn may reuse metadata symbol objects. Compiler/view ownership is still a separate required boundary.
        Assert.Same(first.Compilation, firstEnvironment.GetAnalysisScopes().Single().Compilation);
        Assert.Same(second, secondEnvironment.GetAnalysisScopes().Single().Compilation);
        if (change == "other-pe")
        {
            Assert.Equal(((IAssemblySymbol)first.Compilation.GetAssemblyOrModuleSymbol(reference)!).Identity,
                ((IAssemblySymbol)second.GetAssemblyOrModuleSymbol(otherReference)!).Identity);
            Assert.NotEqual(ExceptionFlowFiniteOrbitAdmission.ScopeDigest(firstEnvironment), ExceptionFlowFiniteOrbitAdmission.ScopeDigest(secondEnvironment));
        }
    }

    [Fact]
    public void CompilationReferenceWithMissingModelIsNotMetadataNoBodyAndGrowthRevealsTheBody()
    {
        var dependency = Dependency();
        var reference = dependency.Compilation.ToMetadataReference();
        var fixture = new AdmissionCharacterization.Fixture([Root("A", "External.Api<int>.Run<string>();")],
            MetadataReferences.Default.Append(reference));
        var run = fixture.Session();
        var method = fixture.Compilation.GetTypeByMetadataName("External.Api`1")!.GetMembers("Run").OfType<IMethodSymbol>().Single();
        Assert.NotEmpty(method.DeclaringSyntaxReferences);
        Assert.Contains(method.Locations, location => location.IsInSource);
        var view = run.Environment.GetAnalysisScopes();
        using (var reads = run.Environment.BeginAdmissionReads())
        {
            Assert.False(Local(method, run.Environment, new(method)).HasBody);
            Assert.False(reads.Reusable);
        }
        run.Context.RegisterSupportingSource(dependency.Compilation);
        Assert.NotSame(view, run.Environment.GetAnalysisScopes());
        using (var reads = run.Environment.BeginAdmissionReads())
        {
            Assert.True(Local(method, run.Environment, new(method)).HasBody);
            Assert.True(reads.Reusable);
            Assert.Contains(reads.Models.Values, compilation => ReferenceEquals(compilation, dependency.Compilation));
        }
    }

    [Fact]
    public void SourceImplicitConstructorAndExpressionGetterCannotUseEmptyMethodSyntaxAsNoBodyProof()
    {
        var fixture = new AdmissionCharacterization.Fixture([("/P5B/Fallback.cs", """
            class Box {
                int field = 1;
                public int Value => field;
            }
            """)]);
        var run = fixture.Session();
        var box = fixture.Compilation.GetTypeByMetadataName("Box")!;
        var constructor = box.InstanceConstructors.Single();
        var getter = box.GetMembers("Value").OfType<IPropertySymbol>().Single().GetMethod!;
        Assert.True(constructor.IsImplicitlyDeclared);
        Assert.Empty(constructor.DeclaringSyntaxReferences);
        Assert.NotEmpty(box.DeclaringSyntaxReferences);
        Assert.NotEmpty(getter.AssociatedSymbol!.DeclaringSyntaxReferences);
        Assert.True(Local(constructor, run.Environment, new(constructor)).HasBody);
        Assert.True(Local(getter, run.Environment, new(getter)).HasBody);
    }

    [Fact]
    public void DocumentationFailureCannotPublishPartialFactsAndRetryReacquiresTheProducer()
    {
        var provider = new RetryDocumentation();
        var reference = Emit(Dependency().Compilation, provider);
        var fixture = new AdmissionCharacterization.Fixture([
            Root("A", "Known(); Failing();"),
            ("/P5B/Failure.cs", "public static partial class Entry { static void Known() {} static void Failing() { Known(); External.Api<int>.Run<string>(); } }")
        ], MetadataReferences.Default.Append(reference));
        var run = fixture.Session();
        var method = fixture.Compilation.GetTypeByMetadataName("External.Api`1")!.GetMembers("Run").OfType<IMethodSymbol>().Single();
        Assert.False(Local(method, run.Environment, new(method)).HasBody);
        Assert.Equal(0, provider.Requests);
        var prefix = new AdmissionObservationRecorder();
        var fragment = new ExceptionFlowSummaryFragment();
        Assert.Throws<DocumentationFailure>(() => ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(
            fixture.Symbol("Failing"), run.Environment, prefix, fragment, new(fixture.Symbol("Failing"))));
        Assert.Single(prefix.Keys);
        Assert.Single(fragment.CallEdges);
        Assert.Empty(prefix.ObservedUncertainty);
        int known = 0;
        var facts = new ExceptionFlowFiniteOrbitAdmission.FactStore(run.Environment, symbol => { if (symbol.Name == "Known") known++; });
        Assert.Throws<DocumentationFailure>(() => ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root("A"), run.Environment, facts));
        Assert.Empty(Entries(facts));
        Assert.Equal(1, known);
        provider.Fail = false;
        var retry = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root("A"), run.Environment, facts);
        Assert.Equal(2, known);
        Assert.False(retry.Outcome.Complete);
        Assert.Contains(retry.Outcome.Reasons, reason => reason.Witness.EndsWith(" / executable body unavailable", StringComparison.Ordinal));
        Assert.True(provider.Requests >= 3);
        Assert.False(Local(method, run.Environment, new(method)).HasBody);
        Assert.NotEmpty(Entries(facts));
    }

    private static (bool HasBody, ExceptionFlowSummaryFragment Fragment, AdmissionObservationRecorder Sink) Local(
        ISymbol definition, ExceptionFlowSemanticEnvironment environment, ExceptionFlowCallContext context)
    {
        var fragment = new ExceptionFlowSummaryFragment();
        var sink = new AdmissionObservationRecorder();
        bool body = ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(definition, environment, sink, fragment, context);
        return (body, fragment, sink);
    }

    // A test oracle over actual declaration properties, not a proposed content-hash/store key.
    private static string SchemaShape(ISymbol symbol)
    {
        var definition = symbol.OriginalDefinition;
        return JsonSerializer.Serialize(new
        {
            definition.Kind, Method = definition is IMethodSymbol method ? method.MethodKind : (MethodKind?)null,
            definition.IsImplicitlyDeclared,
            Declarations = definition.DeclaringSyntaxReferences.Select(reference =>
            {
                var syntax = reference.GetSyntax();
                return new { reference.SyntaxTree.FilePath, reference.Span, syntax.RawKind, Text = syntax.ToFullString() };
            }).ToArray()
        });
    }

    private static IDictionary Entries(ExceptionFlowFiniteOrbitAdmission.FactStore facts)
        => AdmissionCharacterization.Field<IDictionary>(facts, "completed");
    private static Dictionary<string, (ExceptionFlowSummaryFragment Fragment, bool HasBody)> Diagnostics(ExceptionFlowFiniteOrbitAdmission admission)
        => AdmissionCharacterization.Field<Dictionary<string, (ExceptionFlowSummaryFragment Fragment, bool HasBody)>>(admission, "diagnosticDefinitions");
    private static List<Edge> RawEdges(ExceptionFlowFiniteOrbitAdmission admission) => AdmissionCharacterization.Field<List<Edge>>(admission, "edges");
    private static (string Path, string Text) Root(string name, string body) => ($"/P5B/{name}.cs", $$"""
        public static partial class Entry {
            /// <summary>Root {{name}}.</summary>
            public static void {{name}}() { {{body}} }
        }
        """);
    private static AdmissionCharacterization.Fixture Dependency(string name = "P5B.Metadata", string extra = "") => new([
        ("/P5B/Api.cs", $$"""
        namespace External {
            public class Api<T> {
                public Api() {}
                public T Value { get; set; } = default!;
                public static void Run<U>() {}
                {{extra}}
            }
        }
        """)
    ], assemblyName: name);
    private static PortableExecutableReference Emit(Compilation compilation, DocumentationProvider? documentation = null)
    {
        using var image = new MemoryStream();
        Assert.True(compilation.Emit(image).Success);
        return MetadataReference.CreateFromImage(image.ToArray(), documentation: documentation);
    }
    private sealed class DocumentationFailure : System.Exception;
    private sealed class RetryDocumentation : DocumentationProvider
    {
        internal bool Fail { get; set; } = true;
        internal int Requests { get; private set; }
        protected override string GetDocumentationForSymbol(string documentationMemberID, CultureInfo preferredCulture,
            CancellationToken cancellationToken = default)
        {
            Requests++;
            if (Fail) throw new DocumentationFailure();
            return "";
        }
        public override bool Equals(object? obj) => ReferenceEquals(this, obj);
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    }
}
