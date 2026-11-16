using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizerTests.Helpers;
using Xunit.Abstractions;

namespace XMLDocNormalizerTests.Check.Semantic.Exception;

/// <summary>Compares both dispatch orchestrators with a fresh per-target context oracle.</summary>
public sealed class ExceptionFlowDispatchCallSiteReuseTests(ITestOutputHelper output)
{
    private static readonly MethodInfo LegacyDispatch = typeof(ExceptionFlowAnalyzer)
        .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
        .Single(method => method.Name == "AddSummaryInvocationEdges" && method.GetParameters().Length == 7);

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(4, false)]
    [InlineData(2, true)]
    [InlineData(4, true)]
    public void EveryRuntimeTargetKeepsItsOrdinalFactsAndOrderedRawEdge(int targets, bool legacy)
    {
        Fixture fixture = Create(targets, "service.Send(value: new object());");
        Run run = Execute(fixture, legacy);
        Assert.Equal(targets, run.Sink.Contexts.Count);
        Assert.All(run.Sink.Contexts, context => Assert.True(context.GetParameterFacts(0)
            .ContainsAll(ExceptionFlowValueFacts.NonNull)));
        Assert.Equal(Enumerable.Range(0, targets).Select(index => "T" + index),
            run.Sink.Symbols.Select(symbol => symbol.ContainingType.Name));
        Assert.Equal(run.Sink.Symbols, DispatchEdges(run).Select(edge => edge.Target.Symbol));
        Assert.All(run.Sink.Contexts, context => Assert.Empty(context.GetKnownNonNullParameterMembers(0)));
        output.WriteLine("PERF06C_REACHED " + JsonSerializer.Serialize(new
        {
            targets, legacy, callerKey = run.Caller.Key, registered = run.Sink.Contexts.Count,
            edges = DispatchEdges(run).Length, oracle = "Fresh CreateDispatchCallContext before every registration"
        }));
    }

    [Theory]
    [InlineData("service.Send(input);", false)]
    [InlineData("service.Send(input);", true)]
    [InlineData("service.Send(null);", false)]
    [InlineData("object? local = new object(); local = null; service.Send(local);", false)]
    [InlineData("object? local = null; local = new object(); service.Send(local);", true)]
    [InlineData("if (input != null) { service.Send(input); }", true)]
    [InlineData("service.Send(Make(input));", false)]
    public void UnknownWritesFlowAndRecursiveValuesMatchFreshEvaluation(string body, bool legacy)
    {
        Run run = Execute(Create(3, body), legacy);
        Assert.Equal(3, run.Sink.Contexts.Count);
        Assert.Equal(3, DispatchEdges(run).Length);
        output.WriteLine("PERF06C_FACTS " + JsonSerializer.Serialize(new
        {
            body, legacy, keys = run.Sink.Contexts.Select(context => context.Key).ToArray()
        }));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NonemptyCallerContextsRemainSeparateAcrossActivations(bool legacy)
    {
        Fixture fixture = Create(3, "service.Send(input);");
        ExceptionFlowCallContext known = new(fixture.Caller,
            new Dictionary<int, ExceptionFlowValueFacts> { [1] = ExceptionFlowValueFacts.NonNull });
        Run first = Execute(fixture, legacy, known);
        Run unknown = Execute(fixture, legacy);
        Run last = Execute(fixture, legacy, known);
        Assert.All(first.Sink.Contexts.Concat(last.Sink.Contexts), context =>
            Assert.True(context.GetParameterFacts(0).ContainsAll(ExceptionFlowValueFacts.NonNull)));
        Assert.All(unknown.Sink.Contexts, context => Assert.Equal(ExceptionFlowValueFacts.None, context.GetParameterFacts(0)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExposedMemberSetCanChangeBetweenTargetsDespiteAnUnchangedKey(bool legacy)
    {
        Fixture fixture = Create(3, "service.Send(box.Value);");
        IPropertySymbol member = (IPropertySymbol)fixture.Data.Compilation.GetTypeByMetadataName("Box")!
            .GetMembers("Value").Single();
        ExceptionFlowCallContext caller = new(fixture.Caller,
            new Dictionary<int, ExceptionFlowValueFacts>(), [new KeyValuePair<int, ISymbol>(2, member)]);
        string key = caller.Key;
        HashSet<ISymbol> escaped = Assert.IsType<HashSet<ISymbol>>(caller.GetKnownNonNullParameterMembers(2));
        Run run = Execute(fixture, legacy, caller, (_, count) =>
        {
            if (count == 1)
            {
                escaped.Clear();
            }
        });
        Assert.NotEmpty(key);
        Assert.Equal(key, caller.Key);
        Assert.True(run.Sink.Contexts[0].GetParameterFacts(0).ContainsAll(ExceptionFlowValueFacts.NonNull));
        Assert.All(run.Sink.Contexts.Skip(1), context => Assert.Equal(ExceptionFlowValueFacts.None, context.GetParameterFacts(0)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RegistrationCanGrowSupportingSourcesAndExcludeEveryParentRead(bool legacy)
    {
        Fixture fixture = Create(3, "service.Send(new object());");
        (ProjectClosureSemanticContext context, ExceptionFlowSemanticEnvironment environment,
            ExceptionFlowSummaryAnalysisSession _) = fixture.Data.Session();
        using ExceptionFlowSemanticEnvironment.AdmissionReads parent = environment.BeginAdmissionReads();
        using ExceptionFlowSemanticEnvironment.AdmissionReads child = environment.BeginAdmissionReads();
        CSharpCompilation supporting = CSharpCompilation.Create("AdditionalSupportingOwner",
            [CSharpSyntaxTree.ParseText("internal sealed class ExtraSupportingType { }", path: "/PERF06C/Extra.cs")],
            MetadataReferences.Default, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Run run = Execute(fixture, legacy, environment: environment, afterRegistration: (_, count) =>
        {
            if (count == 1)
            {
                context.RegisterSupportingSource(supporting);
                environment.ExcludeAdmissionReuse();
            }
        });
        Assert.False(parent.Reusable);
        Assert.False(child.Reusable);
        Assert.Equal(3, run.Sink.Contexts.Count);
        Assert.Equal(3, DispatchEdges(run).Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RegistrationFailurePreservesTheExactCompletedEdgePrefix(bool legacy)
    {
        Fixture fixture = Create(4, "service.Send(new object());");
        Run run = Prepare(fixture, afterRegistration: (_, count) =>
        {
            if (count == 2)
            {
                throw new RegistrationFailure();
            }
        });
        Assert.Throws<RegistrationFailure>(() => Invoke(run, legacy));
        Assert.Equal(2, run.Sink.Contexts.Count);
        Assert.Same(run.Sink.Symbols[0].OriginalDefinition, Assert.Single(DispatchEdges(run)).Target.Symbol);
        Assert.Equal(4, Execute(fixture, legacy).Sink.Contexts.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnresolvedGenericMethodTargetsKeepTheConstructedDirectFallback(bool legacy)
    {
        Fixture fixture = Create(3, "service.Send<object>(new object());",
            "void Send<T>(T value, int stamp = 0)", "public void Send<U>(U renamed, int other = -1) { }");
        Run run = Execute(fixture, legacy);
        Assert.Single(run.Sink.Contexts);
        Assert.All(run.Sink.Contexts, context =>
        {
            IMethodSymbol invocation = Assert.IsAssignableFrom<IMethodSymbol>(context.InvocationSymbol);
            Assert.Equal(SpecialType.System_Object, Assert.Single(invocation.TypeArguments).SpecialType);
            Assert.True(context.GetParameterFacts(0).ContainsAll(ExceptionFlowValueFacts.NonNull));
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RefOutAndParamsKeepTheirExistingFacts(bool legacy)
    {
        Fixture fixture = Create(3, "object? value = new object(); object? other; service.Send(ref value, out other, new object());",
            "void Send(ref object? value, out object? other, params object?[] rest)",
            "public void Send(ref object? renamed, out object? other, params object?[] rest) { other = null; }");
        Run run = Execute(fixture, legacy);
        Assert.Equal(3, run.Sink.Contexts.Count);
        Assert.All(run.Sink.Contexts, context =>
        {
            Assert.True(context.GetParameterFacts(0).ContainsAll(ExceptionFlowValueFacts.NonNull));
            Assert.Equal(ExceptionFlowValueFacts.None, context.GetParameterFacts(1));
        });
    }

    [Fact]
    public void IdenticalSyntaxInDifferentCompilerOwnersDoesNotShareAnActivation()
    {
        Fixture first = Create(2, "service.Send(new object());");
        Fixture second = Create(2, "service.Send(new object());", owner: "OtherOwner");
        Run a = Execute(first, false);
        Run b = Execute(second, false);
        Assert.NotSame(first.Model.Compilation, second.Model.Compilation);
        Assert.All(a.Sink.Symbols, symbol => Assert.Same(first.Data.Compilation.Assembly, symbol.ContainingAssembly));
        Assert.All(b.Sink.Symbols, symbol => Assert.Same(second.Data.Compilation.Assembly, symbol.ContainingAssembly));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ForeignModelFailsBeforeAnyTargetOrEdgeIsPublished(bool legacy)
    {
        Fixture first = Create(3, "service.Send(new object());");
        Fixture foreign = Create(3, "service.Send(new object());", owner: "ForeignOwner");
        Run run = Prepare(first with { Model = foreign.Model });
        Assert.Throws<ArgumentException>(() => Invoke(run, legacy));
        Assert.Empty(run.Sink.Contexts);
        Assert.Empty(run.Fragment.CallEdges);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ConstructedInterfaceOwnerKeepsAllThreeRuntimeTargets(bool legacy)
    {
        Fixture fixture = Create(3, "service.Send(new object());", genericOwner: true);
        Run run = Execute(fixture, legacy);
        Assert.Equal(SpecialType.System_Object, Assert.Single(fixture.Selected.ContainingType.TypeArguments).SpecialType);
        Assert.Equal(3, run.Sink.Contexts.Count);
        Assert.All(run.Sink.Symbols, target => Assert.Equal(SpecialType.System_Object,
            Assert.Single(target.ContainingType.AllInterfaces.Single().TypeArguments).SpecialType));
    }

    [Fact]
    public void EachDispatchLoopUsesOneLazyFactoryOnlyForEmptyCallerFacts()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        string relative = Path.Combine("src", "XMLDocNormalizer", "Checks", "Infrastructure", "Exception", "Flow",
            "ExceptionFlowAnalyzer.SummaryGraphDispatch.cs");
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, relative)))
        {
            directory = directory.Parent;
        }
        Assert.NotNull(directory);
        MethodDeclarationSyntax[] methods = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(directory!.FullName, relative)))
            .GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(method => method.Identifier.ValueText == "AddSummaryInvocationEdges").ToArray();
        Assert.Equal(2, methods.Length);
        foreach (MethodDeclarationSyntax method in methods)
        {
            ForEachStatementSyntax loop = Assert.Single(method.DescendantNodes().OfType<ForEachStatementSyntax>());
            VariableDeclaratorSyntax snapshot = Assert.Single(method.Body!.Statements.OfType<LocalDeclarationStatementSyntax>()
                .SelectMany(statement => statement.Declaration.Variables).Where(variable => variable.Identifier.ValueText == "callSiteContext"));
            Assert.Equal("null", snapshot.Initializer!.Value.ToString());
            Assert.True(snapshot.SpanStart < loop.SpanStart);
            AssignmentExpressionSyntax lazy = Assert.Single(loop.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                .Where(assignment => assignment.IsKind(SyntaxKind.CoalesceAssignmentExpression)));
            Assert.Equal("callSiteContext", lazy.Left.ToString());
            Assert.StartsWith("ExceptionFlowContextualFactEvaluator.CreateCallContext(", lazy.Right.ToString());
            IfStatementSyntax guard = lazy.Ancestors().OfType<IfStatementSyntax>().First();
            Assert.Contains("CallerContext.Key.Length == 0", guard.Condition.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains(guard.Statement.DescendantNodes().OfType<InvocationExpressionSyntax>(), call =>
                call.Expression.ToString() == "CreateDispatchTargetContext"
                && call.ArgumentList.Arguments.Last().Expression.ToString() == "callSiteContext");
            Assert.Contains(guard.Else!.DescendantNodes().OfType<InvocationExpressionSyntax>(), call =>
                call.Expression.ToString() == "CreateDispatchCallContext");
            Assert.True(guard.Span.End < loop.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .First(call => call.Expression.ToString().EndsWith("RegisterMethodTarget", StringComparison.Ordinal)).SpanStart);
        }
    }

    [Fact]
    public void MissingRuntimeTargetsKeepTheExistingDirectFallback()
    {
        Run run = Execute(Create(0, "service.Send(new object());"), false);
        Assert.Single(run.Fragment.CallEdges);
        Assert.Equal("IService", Assert.Single(run.Fragment.CallEdges).Target.Symbol.ContainingType.Name);
        Assert.Empty(run.Fragment.UncertainTargets);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AbstractVirtualDispatchKeepsAllOverrides(bool legacy)
    {
        Run run = Execute(Create(3, "service.Send(new object());", virtualDispatch: true), legacy);
        Assert.Equal(3, run.Sink.Contexts.Count);
        Assert.Equal(3, DispatchEdges(run).Length);
        Assert.All(run.Sink.Symbols, target => Assert.True(target.IsOverride));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void FullAdmissionFindingsCoverageAndCanonicalProofHaveBaselineEvidence(int targets)
    {
        Fixture fixture = Create(targets, "service.Send(new object());", implementation:
            "public void Send(object? renamed, int other = -1) { if (renamed == null) { throw new System.ArgumentException(); } throw new System.InvalidOperationException(); }");
        (ProjectClosureSemanticContext context, ExceptionFlowSemanticEnvironment _,
            ExceptionFlowSummaryAnalysisSession session) = fixture.Data.Session();
        string capture = fixture.Data.Capture("Entry", context, session);
        Assert.Contains("InvalidOperationException", capture);
        output.WriteLine("PERF06C_PARITY " + JsonSerializer.Serialize(new { targets, capture }));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void DocumentedMultiTargetCallReportsOnlyTheEscapingException(int targets)
    {
        Fixture fixture = Create(targets, "service.Send(\"ready\", failure, rejected);",
            "void Send(object? value, System.InvalidOperationException failure, System.ArgumentException rejected)",
            "public void Send(object? renamed, System.InvalidOperationException failure, System.ArgumentException rejected) { if (renamed == null) { throw rejected; } throw failure; }",
            documented: true);
        Run dispatch = Execute(fixture, false);
        Assert.Equal(targets, dispatch.Sink.Contexts.Count);
        (ProjectClosureSemanticContext context, ExceptionFlowSemanticEnvironment _,
            ExceptionFlowSummaryAnalysisSession session) = fixture.Data.Session();
        string capture = fixture.Data.Capture("Entry", context, session);
        using JsonDocument json = JsonDocument.Parse(capture);
        JsonElement finding = Assert.Single(json.RootElement.GetProperty("Findings").EnumerateArray());
        Assert.Contains("InvalidOperationException", finding.GetProperty("Message").GetString());
        Assert.DoesNotContain("ArgumentException", finding.GetProperty("Message").GetString());
        Assert.Equal("/PERF06C/Entry.cs", finding.GetProperty("FilePath").GetString());
        Assert.Equal("Complete", json.RootElement.GetProperty("Coverage").GetProperty("Completion").GetString());
        output.WriteLine("PERF06C_FINDINGS " + JsonSerializer.Serialize(new { targets, capture }));
    }

    private static ExceptionFlowSummaryCallEdge[] DispatchEdges(Run run) => run.Fragment.CallEdges
        .Where(edge => edge.Target.Symbol.Name == "Send").ToArray();

    private static Run Execute(Fixture fixture, bool legacy, ExceptionFlowCallContext? caller = null,
        Action<ExceptionFlowCallContext, int>? afterRegistration = null, ExceptionFlowSemanticEnvironment? environment = null)
    {
        Run run = Prepare(fixture, caller, afterRegistration, environment);
        Invoke(run, legacy);
        return run;
    }

    private static Run Prepare(Fixture fixture, ExceptionFlowCallContext? caller = null,
        Action<ExceptionFlowCallContext, int>? afterRegistration = null, ExceptionFlowSemanticEnvironment? environment = null)
    {
        ExceptionFlowCallContext actualCaller = caller ?? new(fixture.Caller);
        RecordingSink sink = new(fixture, actualCaller, afterRegistration);
        return new(fixture, actualCaller, sink, new(), environment ?? fixture.Data.Session().Environment);
    }

    private static void Invoke(Run run, bool legacy)
    {
        if (!legacy)
        {
            ExceptionFlowAnalyzer.AnalyzeSummaryInvocations(run.Fixture.Root.Body!, run.Fixture.Model,
                run.Environment, run.Sink, run.Fragment, run.Caller);
            return;
        }
        try
        {
            LegacyDispatch.Invoke(null, [run.Fixture.Invocation, run.Fixture.Selected, run.Fixture.Model,
                run.Environment, run.Sink, run.Fragment, run.Caller]);
        }
        catch (TargetInvocationException error) when (error.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static Fixture Create(int targets, string body,
        string contract = "void Send(object? value, int stamp = 0)",
        string implementation = "public void Send(object? renamed, int other = -1) { }", string owner = "DispatchOwner",
        bool virtualDispatch = false, bool genericOwner = false, bool documented = false)
    {
        string entry = "#nullable enable\ninternal static class EntryType { internal static void Entry(IService service, object? input, Box box) { " + body + " } }";
        string contracts = "#nullable enable\ninternal interface IService { " + contract + "; }\n" +
            "internal sealed class Box { public object? Value { get; } }\n" +
            "internal static class Helper { internal static object? Make(object? input) { return Make(input); } }";
        entry = entry.Replace("Make(input)", "Helper.Make(input)", StringComparison.Ordinal);
        if (documented)
        {
            entry = entry.Replace("internal static void Entry", "\n/// <summary>Exercises runtime dispatch.</summary>\ninternal static void Entry", StringComparison.Ordinal);
            entry = entry.Replace("Box box)", "Box box, System.InvalidOperationException failure, System.ArgumentException rejected)", StringComparison.Ordinal);
        }
        string implementations = "#nullable enable\n" + string.Join("\n", Enumerable.Range(0, targets)
            .Select(index => "internal sealed class T" + index + " : IService { " + implementation + " }"));
        if (virtualDispatch)
        {
            contracts = contracts.Replace("interface IService { void Send", "abstract class IService { public abstract void Send", StringComparison.Ordinal);
            implementations = implementations.Replace("public void Send", "public override void Send", StringComparison.Ordinal);
        }
        if (genericOwner)
        {
            entry = entry.Replace("IService service", "IService<object> service", StringComparison.Ordinal);
            contracts = contracts.Replace("interface IService {", "interface IService<T> {", StringComparison.Ordinal);
            implementations = implementations.Replace(": IService {", ": IService<object> {", StringComparison.Ordinal);
        }
        AdmissionCharacterization.Fixture data = new([
            ("/PERF06C/Entry.cs", entry), ("/PERF06C/Contracts.cs", contracts),
            ("/PERF06C/Targets.cs", implementations)], assemblyName: owner);
        MethodDeclarationSyntax root = data.Root("Entry");
        SemanticModel model = data.Compilation.GetSemanticModel(root.SyntaxTree);
        InvocationExpressionSyntax invocation = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Single(call => call.Expression is MemberAccessExpressionSyntax member && member.Name.Identifier.ValueText == "Send");
        return new(data, root, model, data.Symbol("Entry"), invocation, (IMethodSymbol)model.GetSymbolInfo(invocation).Symbol!);
    }

    private sealed class RecordingSink(Fixture fixture, ExceptionFlowCallContext caller,
        Action<ExceptionFlowCallContext, int>? afterRegistration) : IExceptionFlowSummaryTargetSink
    {
        internal List<ExceptionFlowCallContext> Contexts { get; } = [];
        internal List<IMethodSymbol> Symbols { get; } = [];

        public ExceptionFlowCallableKey RegisterTarget(ISymbol symbol, ExceptionFlowCallContext context)
        {
            if (symbol is IMethodSymbol { Name: "Send" } target)
            {
                ExceptionFlowCallContext fresh = ExceptionFlowAnalyzer.CreateDispatchCallContext(fixture.Selected,
                    target, fixture.Invocation.ArgumentList.Arguments, fixture.Model, caller);
                Assert.Equal(fresh.Key, context.Key);
                Assert.Equal(JsonSerializer.Serialize(fresh.CanonicalContext), JsonSerializer.Serialize(context.CanonicalContext));
                Assert.True(SymbolEqualityComparer.Default.Equals(fresh.InvocationSymbol, context.InvocationSymbol));
                Contexts.Add(context);
                Symbols.Add(target);
                afterRegistration?.Invoke(context, Contexts.Count);
            }
            return new(symbol, context);
        }

        public void ObserveLocalFragment(ExceptionFlowSummaryFragment fragment) { }
    }

    private sealed class RegistrationFailure : System.Exception;
    private sealed record Fixture(AdmissionCharacterization.Fixture Data, MethodDeclarationSyntax Root,
        SemanticModel Model, IMethodSymbol Caller, InvocationExpressionSyntax Invocation, IMethodSymbol Selected);
    private sealed record Run(Fixture Fixture, ExceptionFlowCallContext Caller, RecordingSink Sink,
        ExceptionFlowSummaryFragment Fragment, ExceptionFlowSemanticEnvironment Environment);
}
