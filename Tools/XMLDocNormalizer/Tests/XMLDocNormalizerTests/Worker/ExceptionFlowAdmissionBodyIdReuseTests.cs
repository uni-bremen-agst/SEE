using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;
using static XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.ExceptionFlowFiniteOrbitProfile;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Protects exact-original Body-ID reuse without changing registration effects.</summary>
public sealed class ExceptionFlowAdmissionBodyIdReuseTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string Code = """
        namespace BodyIdFixture;
        public class Owner<T>
        {
            public Owner() { }
            public T Property { get; set; } = default!;
            public void Plain() { }
            public void Other() { }
            public void Generic<U>(U value) { }
            public void WithLocal() { void Local() { } Local(); }
        }
        public static class Extensions { public static void Extend<T>(this Owner<T> owner) { } }
        public class Entry { public static void Root() { } }
        """;

    [Theory]
    [InlineData("same", true)]
    [InlineData("constructed", true)]
    [InlineData("constructed-different-bindings", true)]
    [InlineData("reduced", true)]
    [InlineData("property", true)]
    [InlineData("getter", true)]
    [InlineData("setter", true)]
    [InlineData("constructor", true)]
    [InlineData("local", true)]
    [InlineData("local-distinct-models", false)]
    [InlineData("different", false)]
    [InlineData("reduced-static", false)]
    [InlineData("equal-display-clone", false)]
    [InlineData("foreign-owner", false)]
    public void ExactOriginalsReuseTheSuccessfulIdAndAllOtherCasesUseTheExistingComparison(string shape, bool identical)
    {
        AdmissionCharacterization.Fixture fixture = Fixture();
        (ISymbol target, ISymbol invocation) = Symbols(fixture, shape);
        Assert.Equal(identical, ReferenceEquals(target.OriginalDefinition, invocation.OriginalDefinition));
        string expectedTarget = ExceptionFlowFiniteOrbitAdmission.BodyId(target.OriginalDefinition);
        string expectedInvocation = ExceptionFlowFiniteOrbitAdmission.BodyId(invocation);
        if (identical)
        {
            Assert.Same(target.OriginalDefinition, target.OriginalDefinition.OriginalDefinition);
            Assert.Equal(Encoding.UTF8.GetBytes(expectedTarget), Encoding.UTF8.GetBytes(expectedInvocation));
        }
        if (shape == "equal-display-clone")
        {
            Assert.NotSame(target.ContainingAssembly, invocation.ContainingAssembly);
            Assert.Equal(target.ToDisplayString(), invocation.ToDisplayString());
            Assert.Equal(expectedTarget, expectedInvocation);
        }

        int computations = 0;
        bool mismatch = ActualComparison()(target, invocation, expectedTarget, symbol =>
        {
            computations++;
            return ExceptionFlowFiniteOrbitAdmission.BodyId(symbol);
        });
        Assert.Equal(expectedTarget != expectedInvocation, mismatch);
        Assert.Equal(identical ? 0 : 1, computations);

        ExceptionFlowFiniteOrbitAdmission admission = Create(fixture);
        ExceptionFlowCallContext context = new ExceptionFlowCallContext(target).WithInvocation(invocation);
        ExceptionFlowCallableKey key = admission.RegisterTarget(target, context);
        Assert.Same(target.OriginalDefinition, key.Symbol);
        Assert.Same(context, Field<ExceptionFlowCallContext>(key, "callContext"));
        List<Reason> reasons = Field<List<Reason>>(admission, "reasons");
        if (mismatch)
        {
            Assert.Contains(new Reason("OwnerBindingFailure", "Source/metadata declaration mismatch: " + expectedInvocation + " -> " + expectedTarget), reasons);
        }
        else
        {
            Assert.Empty(reasons);
        }
        Edge edge = Assert.Single(Field<List<Edge>>(admission, "edges"));
        Assert.Equal(expectedTarget, edge.To);
        string invocationKey = (string)typeof(ExceptionFlowFiniteOrbitAdmission).GetMethod("InvocationId", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, [invocation])!;
        string caller = Field<string>(admission, "currentDefinition");
        Assert.Equal(JsonSerializer.Serialize(new[] { caller, expectedTarget, invocationKey }), edge.Witness);
        Assert.Equal(2, admission.DefinitionCount);
        Assert.Equal(2, Field<Queue<string>>(admission, "pendingDefinitions").Count);
        Assert.Single(Field<Dictionary<string, Compilation?>>(admission, "metadataBindings"));
        Assert.Equal(ExceptionFlowInvocationSubstitution.Bindings(invocation).Count(), edge.Assignments.Length);
    }

    [Fact]
    public void ConstructedInvocationsKeepDifferentAssignmentsAndTransitionProvenance()
    {
        AdmissionCharacterization.Fixture fixture = Fixture();
        (ISymbol symbol, ISymbol first) = Symbols(fixture, "constructed");
        IMethodSymbol method = (IMethodSymbol)symbol;
        ISymbol second = method.Construct(fixture.Compilation.GetSpecialType(SpecialType.System_Int32));
        ExceptionFlowFiniteOrbitAdmission admission = Create(fixture);
        admission.RegisterTarget(symbol, new ExceptionFlowCallContext(symbol).WithInvocation(first));
        admission.RegisterTarget(symbol, new ExceptionFlowCallContext(symbol).WithInvocation(second));
        Edge[] edges = Field<List<Edge>>(admission, "edges").ToArray();
        Assert.Equal(2, edges.Length);
        Assert.Equal(edges[0].To, edges[1].To);
        Assert.NotEqual(JsonSerializer.Serialize(edges[0].Assignments), JsonSerializer.Serialize(edges[1].Assignments));
        Assert.NotEqual(edges[0].Witness, edges[1].Witness);
        Assert.Equal(2, Field<HashSet<string>>(admission, "edgeTemplates").Count);
    }

    [Fact]
    public void RepeatedRegistrationStillRepairsConsumerAndKeepsEveryRawEdgeAndFirstLocation()
    {
        AdmissionCharacterization.Fixture fixture = Fixture();
        (ISymbol target, ISymbol invocation) = Symbols(fixture, "same");
        ExceptionFlowFiniteOrbitAdmission admission = Create(fixture);
        HashSet<string> consumer = Field<HashSet<string>>(admission, "knownAssemblies");
        string[] required = consumer.ToArray();
        ExceptionFlowCallContext context = new ExceptionFlowCallContext(target).WithInvocation(invocation);
        ExceptionFlowCallableKey first = admission.RegisterTarget(target, context);
        consumer.Clear();
        consumer.Add("sentinel");
        ExceptionFlowCallableKey second = admission.RegisterTarget(target, context);
        Assert.NotSame(first, second);
        Assert.All(required, value => Assert.Contains(value, consumer));
        Assert.Contains("sentinel", consumer);
        ExceptionFlowSummaryFragment fragment = new();
        fragment.AddCallEdge(new(first, new(ExceptionFlowPathStepKind.MethodCall, "first", "/PERF10C/Call.cs", 10, 2)));
        fragment.AddCallEdge(new(second, new(ExceptionFlowPathStepKind.MethodCall, "second", "/PERF10C/Call.cs", 20, 3)));
        admission.ObserveLocalFragment(fragment);
        Edge[] edges = Field<List<Edge>>(admission, "edges").ToArray();
        Assert.Equal(2, edges.Length);
        Assert.Contains("\"Line\":10", edges[0].Witness, StringComparison.Ordinal);
        Assert.Contains("\"Line\":20", edges[1].Witness, StringComparison.Ordinal);
        Assert.Equal(2, edges.Distinct().Count());
        Assert.Equal(2, admission.DefinitionCount);
    }

    [Fact]
    public void EqualCanonicalNamesFromDifferentCompilerOwnersStillRecordTheDeclarationConflict()
    {
        AdmissionCharacterization.Fixture fixture = Fixture();
        (ISymbol original, ISymbol clone) = Symbols(fixture, "equal-display-clone");
        ExceptionFlowFiniteOrbitAdmission admission = Create(fixture);
        admission.RegisterTarget(original, new(original));
        admission.RegisterTarget(clone, new(clone));
        Assert.Contains(Field<List<Reason>>(admission, "reasons"), reason => reason.Category == "OwnerBindingFailure"
            && reason.Witness.StartsWith("Multiple source declarations for ", StringComparison.Ordinal));
        Assert.Equal(2, Field<List<Edge>>(admission, "edges").Count);
    }

    [Fact]
    public void MissingBodyFrameStillFailsBeforeUnsupportedInvocationIdentityAndKeepsPriorRegistrationEffects()
    {
        AdmissionCharacterization.Fixture fixture = Fixture();
        (ISymbol target, ISymbol _) = Symbols(fixture, "same");
        ISymbol unsupported = fixture.Compilation.GetTypeByMetadataName("BodyIdFixture.Owner`1")!.TypeParameters[0];
        ExceptionFlowCallContext context = new ExceptionFlowCallContext(target).WithInvocation(unsupported);
        ExceptionFlowFiniteOrbitAdmission admission = Create(fixture);
        typeof(ExceptionFlowFiniteOrbitAdmission).GetField("currentDefinition", Hidden)!.SetValue(admission, null);
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => admission.RegisterTarget(target, context));
        Assert.Equal("No discovery body frame.", failure.Message);
        Assert.Equal(2, admission.DefinitionCount);
        Assert.Empty(Field<List<Edge>>(admission, "edges"));
        typeof(ExceptionFlowFiniteOrbitAdmission).GetField("currentDefinition", Hidden)!.SetValue(admission, "caller");
        Assert.Throws<NotSupportedException>(() => admission.RegisterTarget(target, context));
        Assert.Empty(Field<List<Edge>>(admission, "edges"));
    }

    [Theory]
    [InlineData("complete")]
    [InlineData("outside")]
    [InlineData("generic")]
    [InlineData("recursive")]
    public void DiscoveryPreservesCanonicalJsonDigestReasonsAndIncompleteEvidence(string shape)
    {
        string members = shape switch
        {
            "complete" => "static void Root() { Leaf(); Leaf(); } static void Leaf() { }",
            "outside" => "static void Root() { Missing(); } static extern void Missing();",
            "generic" => "static void Root() { Leaf<int>(); Leaf<string>(); } static void Leaf<T>() { throw new Fault<T>(); }",
            "recursive" => "static void Root() { Leaf<int>(); } static void Leaf<T>() { Leaf<System.Collections.Generic.List<T>>(); }",
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };
        AdmissionCharacterization.Fixture fixture = new([("/PERF10C/Discovery.cs", "class Fault<T> : System.Exception {} class Entry { " + members + " }")]);
        ExceptionFlowSemanticEnvironment environment = fixture.Session().Environment;
        ExceptionFlowFiniteOrbitAdmission admission = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root("Root"), environment);
        Dictionary<string, ISymbol> declarations = Field<Dictionary<string, ISymbol>>(admission, "declarations");
        Dictionary<string, List<Term>> templates = Field<Dictionary<string, List<Term>>>(admission, "templates");
        Body[] bodies = declarations.Select(pair => new Body(pair.Key,
            ExceptionFlowInvocationSubstitution.Bindings(pair.Value).Select(binding => ExceptionFlowInvocationSubstitution.Binder(binding.Formal)).ToArray(),
            templates[pair.Key].DistinctBy(term => term.Key).OrderBy(term => term.Key, StringComparer.Ordinal).ToArray())).ToArray();
        Certificate expected = ExceptionFlowFiniteOrbitProfile.Check(bodies, Field<List<Edge>>(admission, "edges").Distinct().ToArray(),
            admission.Outcome.Root, [], admission.Outcome.ScopeDigest, Field<List<Reason>>(admission, "reasons"));
        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(admission.Outcome));
        Assert.Equal(Encoding.UTF8.GetBytes(expected.InventoryWitness), Encoding.UTF8.GetBytes(admission.Outcome.InventoryWitness));
        Assert.Equal(Digest(expected.InventoryWitness), admission.Outcome.InventoryDigest);
        Assert.Equal(expected.Reasons, admission.Outcome.Reasons);
        Assert.Equal(shape is "complete" or "generic", admission.Outcome.Complete);
        if (!admission.Outcome.Complete)
        {
            Assert.True(admission.Outcome.MayMiss);
            Assert.True(admission.Outcome.MayAdd);
            Assert.True(admission.Outcome.NormalFindingAdmissionBlocked);
        }
    }

    private static AdmissionCharacterization.Fixture Fixture(string assembly = "PERF10C.Test")
        => new([("/PERF10C/Fixture.cs", Code)], assemblyName: assembly);

    private static (ISymbol Target, ISymbol Invocation) Symbols(AdmissionCharacterization.Fixture fixture, string shape)
    {
        INamedTypeSymbol owner = fixture.Compilation.GetTypeByMetadataName("BodyIdFixture.Owner`1")!.Construct(fixture.Compilation.GetSpecialType(SpecialType.System_Int32));
        IMethodSymbol plain = (IMethodSymbol)owner.GetMembers("Plain").Single();
        IMethodSymbol generic = (IMethodSymbol)owner.GetMembers("Generic").Single();
        IPropertySymbol property = (IPropertySymbol)owner.GetMembers("Property").Single();
        IMethodSymbol extension = (IMethodSymbol)fixture.Compilation.GetTypeByMetadataName("BodyIdFixture.Extensions")!.GetMembers("Extend").Single();
        IMethodSymbol reduced = extension.ReduceExtensionMethod(owner)!;
        ISymbol Local()
        {
            LocalFunctionStatementSyntax local = fixture.Compilation.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single();
            return fixture.Compilation.GetSemanticModel(local.SyntaxTree).GetDeclaredSymbol(local)!;
        }
        ISymbol Clone(string assembly)
        {
            AdmissionCharacterization.Fixture other = Fixture(assembly);
            return other.Compilation.GetTypeByMetadataName("BodyIdFixture.Owner`1")!.Construct(other.Compilation.GetSpecialType(SpecialType.System_Int32)).GetMembers("Plain").Single();
        }
        (ISymbol, ISymbol) SameLocal()
        {
            ISymbol local = Local();
            return (local, local);
        }
        return shape switch
        {
            "same" => (plain, plain),
            "constructed" => (generic, generic.Construct(fixture.Compilation.GetSpecialType(SpecialType.System_String))),
            "constructed-different-bindings" => (generic.Construct(fixture.Compilation.GetSpecialType(SpecialType.System_String)), generic.Construct(fixture.Compilation.GetSpecialType(SpecialType.System_Int32))),
            "reduced" => (reduced, reduced),
            "property" => (property, property),
            "getter" => (property.GetMethod!, property.GetMethod!),
            "setter" => (property.SetMethod!, property.SetMethod!),
            "constructor" => (owner.InstanceConstructors.Single(), owner.InstanceConstructors.Single()),
            "local" => SameLocal(),
            "local-distinct-models" => (Local(), Local()),
            "different" => (plain, owner.GetMembers("Other").Single()),
            "reduced-static" => (extension, reduced),
            "equal-display-clone" => (plain, Clone("PERF10C.Test")),
            "foreign-owner" => (plain, Clone("PERF10C.Foreign")),
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };
    }

    private static ExceptionFlowFiniteOrbitAdmission Create(AdmissionCharacterization.Fixture fixture)
    {
        ExceptionFlowFiniteOrbitAdmission admission = (ExceptionFlowFiniteOrbitAdmission)Activator.CreateInstance(typeof(ExceptionFlowFiniteOrbitAdmission), Hidden, null, [fixture.Session().Environment], null)!;
        ISymbol root = fixture.Compilation.GetTypeByMetadataName("BodyIdFixture.Entry")!.GetMembers("Root").Single();
        string id = (string)typeof(ExceptionFlowFiniteOrbitAdmission).GetMethod("AddDefinition", Hidden)!.Invoke(admission, [root])!;
        typeof(ExceptionFlowFiniteOrbitAdmission).GetField("currentDefinition", Hidden)!.SetValue(admission, id);
        return admission;
    }

    private static T Field<T>(object instance, string name) => AdmissionCharacterization.Field<T>(instance, name);

    private static Func<ISymbol, ISymbol, string, Func<ISymbol, string>, bool> ActualComparison()
    {
        string directory = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(directory, "XMLDocNormalizer.sln")))
        {
            directory = Directory.GetParent(directory)?.FullName ?? throw new InvalidOperationException("Tool root missing.");
        }
        string path = Path.Combine(directory, "src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowFiniteOrbitAdmission.cs");
        MethodDeclarationSyntax method = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(node => node.Identifier.ValueText == "RegisterTarget");
        IfStatementSyntax comparison = method.Body!.Statements.OfType<IfStatementSyntax>().Single(node => node.Condition.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(call => call.Expression.ToString() == "BodyId"));
        string code = "using System; using Microsoft.CodeAnalysis; public static class ActualBodyIdComparison { public static bool Run(ISymbol symbol, ISymbol invocation, string target, Func<ISymbol,string> BodyId) => " + comparison.Condition + "; }";
        CSharpCompilation compilation = CSharpCompilation.Create("PERF10C.Comparison." + Guid.NewGuid().ToString("N"), [CSharpSyntaxTree.ParseText(code)],
            MetadataReferences.Default.Concat([MetadataReference.CreateFromFile(typeof(ISymbol).Assembly.Location)]), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using MemoryStream image = new();
        Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(image);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        return Assembly.Load(image.ToArray()).GetType("ActualBodyIdComparison")!.GetMethod("Run")!.CreateDelegate<Func<ISymbol, ISymbol, string, Func<ISymbol, string>, bool>>();
    }
}
