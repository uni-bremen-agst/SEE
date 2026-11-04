using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using XMLDocNormalizer.Checks.Infrastructure.Exception;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;
using Xunit.Abstractions;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Source-backed C3G proof obligations; no new analyzer or productive proof recognizer.</summary>
public sealed class ExceptionFlowPositiveWitnessCompositionContractTests(ITestOutputHelper output)
{
    [Fact]
    public void BoundCoreLibraryHasTheRequiredTrivialObjectConstructorAndNoHelperInitializer()
    {
        var compilation = Fixture(Original()).Compilation;
        var objectType = compilation.GetSpecialType(SpecialType.System_Object);
        var reference = Assert.IsAssignableFrom<PortableExecutableReference>(compilation.GetMetadataReference(objectType.ContainingAssembly));
        Assert.True(ReferenceEquals(compilation.GetAssemblyOrModuleSymbol(reference), objectType.ContainingAssembly));
        Assert.Equal(typeof(object).Assembly.Location, reference.FilePath);
        var metadata = Assert.IsType<AssemblyMetadata>(reference.GetMetadata());
        var reader = Assert.Single(metadata.GetModules()).GetMetadataReader();
        Guid mvid = reader.GetGuid(reader.GetModuleDefinition().Mvid);
        Assert.Equal(typeof(object).Assembly.ManifestModule.ModuleVersionId, mvid);
        Assert.DoesNotContain(typeof(object).Assembly.GetCustomAttributesData(), attribute =>
            attribute.AttributeType.FullName == "System.Runtime.CompilerServices.ReferenceAssemblyAttribute");
        byte[]? il = typeof(object).GetConstructor(Type.EmptyTypes)!.GetMethodBody()!.GetILAsByteArray();
        Assert.NotNull(il);
        Assert.Equal(new byte[] { 0x2A }, il!.Where(value => value != 0x00).ToArray());
        foreach (string name in new[] { "Object", "ArgumentNullException" })
        {
            var handle = Assert.Single(reader.TypeDefinitions, handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == name
                && reader.GetString(reader.GetTypeDefinition(handle).Namespace) == "System");
            Assert.DoesNotContain(reader.GetTypeDefinition(handle).GetMethods(), method => reader.GetString(reader.GetMethodDefinition(method).Name) == ".cctor");
        }
        Capture("bound-corelib-foundation", new { Assembly = objectType.ContainingAssembly.Identity.ToString(), Mvid = mvid,
            ObjectConstructorIL = Convert.ToHexString(il), NoObjectOrGuardTypeInitializer = true,
            ExactExistingCompilerReference = true, PublicRuntimeMetadataOnly = true,
            NotAProductionILAnalyzerOrNewFrameworkExceptionFact = true });
    }

    [Fact]
    public void ExactOriginalHasOneCompatibleFiniteNormalPrefixAndNullArgumentChain()
    {
        string source = Original();
        Assert.Equal("6AFA959A9CB2208BB4222A99BD716A644E1F6EA9994DB3D28C2E878336A528DB", Hash(source));
        var fixture = Fixture(source);
        var root = fixture.Root("M");
        var model = fixture.Compilation.GetSemanticModel(root.SyntaxTree);
        var owner = fixture.Symbol("M").ContainingType;
        var field = Assert.Single(owner.GetMembers().OfType<IFieldSymbol>());
        Assert.True(field.IsStatic && field.IsReadOnly);
        Assert.Equal("Cache", field.Name);
        var variable = Assert.IsType<VariableDeclaratorSyntax>(Assert.Single(field.DeclaringSyntaxReferences).GetSyntax());
        var allocation = Assert.IsAssignableFrom<IObjectCreationOperation>(model.GetOperation(variable.Initializer!.Value));
        var allocated = Assert.IsAssignableFrom<INamedTypeSymbol>(allocation.Type);
        Assert.True(SymbolEqualityComparer.Default.Equals(field.Type, allocated));
        Assert.True(allocated.IsSealed);
        Assert.True(allocation.Constructor!.IsImplicitlyDeclared);
        Assert.Equal(SpecialType.System_Object, allocated.BaseType!.SpecialType);
        var table = Assert.IsType<ClassDeclarationSyntax>(Assert.Single(allocated.OriginalDefinition.DeclaringSyntaxReferences).GetSyntax());
        Assert.Single(table.Members);
        Assert.IsType<MethodDeclarationSyntax>(table.Members[0]);
        Assert.Empty(table.Members.OfType<BaseFieldDeclarationSyntax>());
        Assert.Empty(table.Members.OfType<ConstructorDeclarationSyntax>());
        Assert.Empty(allocated.StaticConstructors);
        Assert.True(SymbolEqualityComparer.Default.Equals(allocated.ContainingAssembly, fixture.Compilation.Assembly));
        var ownerSource = Assert.IsType<ClassDeclarationSyntax>(Assert.Single(owner.DeclaringSyntaxReferences).GetSyntax());
        Assert.Empty(ownerSource.Members.OfType<ConstructorDeclarationSyntax>());
        Assert.Single(ownerSource.Members.OfType<FieldDeclarationSyntax>());
        Assert.Empty(root.DescendantNodes().OfType<TryStatementSyntax>());
        Assert.Equal(2, root.Body!.Statements.Count);

        var getSyntax = Assert.Single(root.DescendantNodes().OfType<InvocationExpressionSyntax>(), n => n.Expression is MemberAccessExpressionSyntax);
        var get = Assert.IsAssignableFrom<IInvocationOperation>(model.GetOperation(getSyntax));
        Assert.True(SymbolEqualityComparer.Default.Equals(get.TargetMethod.ContainingType, allocated));
        Assert.False(get.TargetMethod.IsVirtual);
        Assert.False(ExceptionFlowConditionalWeakTableValueFactsProvider.TryGetConditionalWeakTableGetValueParts(getSyntax, model, out _, out _));
        var getSource = Assert.IsType<MethodDeclarationSyntax>(Assert.Single(get.TargetMethod.DeclaringSyntaxReferences).GetSyntax());
        var returned = Assert.IsType<ReturnStatementSyntax>(Assert.Single(getSource.Body!.Statements));
        Assert.True(returned.Expression!.IsKind(SyntaxKind.DefaultLiteralExpression));
        // The exact sole statement cannot invoke either incoming argument or mutate the receiver.
        Assert.Empty(getSource.Body.DescendantNodes().OfType<InvocationExpressionSyntax>());
        Assert.Empty(getSource.Body.DescendantNodes().OfType<AssignmentExpressionSyntax>());
        var returnType = new ExceptionFlowInvocationSubstitution(get.TargetMethod, fixture.Compilation)
            .Apply(get.TargetMethod.OriginalDefinition.ReturnType);
        Assert.True(returnType.IsReferenceType);
        Assert.True(SymbolEqualityComparer.Default.Equals(returnType, get.TargetMethod.ReturnType));

        var key = Assert.IsAssignableFrom<IObjectCreationOperation>(get.Arguments.Single(a => a.Parameter!.Ordinal == 0).Value);
        var keyType = Assert.IsAssignableFrom<INamedTypeSymbol>(key.Type);
        var keySource = Assert.IsType<ClassDeclarationSyntax>(Assert.Single(keyType.DeclaringSyntaxReferences).GetSyntax());
        Assert.Empty(keySource.Members);
        Assert.True(key.Constructor!.IsImplicitlyDeclared);
        Assert.Equal(SpecialType.System_Object, keyType.BaseType!.SpecialType);
        var lambda = Assert.Single(getSyntax.DescendantNodes().OfType<SimpleLambdaExpressionSyntax>());
        Assert.Contains(lambda.Modifiers, token => token.IsKind(SyntaxKind.StaticKeyword));

        var local = Assert.IsAssignableFrom<ILocalSymbol>(model.GetDeclaredSymbol(Assert.Single(root.DescendantNodes().OfType<VariableDeclaratorSyntax>())));
        var callSyntax = Assert.IsType<ExpressionStatementSyntax>(root.Body.Statements[1]).Expression;
        var validate = Assert.IsAssignableFrom<IInvocationOperation>(model.GetOperation(callSyntax));
        Assert.Equal("Validate", validate.TargetMethod.Name);
        Assert.True(SymbolEqualityComparer.Default.Equals(owner, validate.TargetMethod.ContainingType));
        var passed = Assert.IsAssignableFrom<ILocalReferenceOperation>(Assert.Single(validate.Arguments).Value);
        Assert.True(SymbolEqualityComparer.Default.Equals(local, passed.Local));
        Assert.Equal(0, Assert.Single(validate.Arguments).Parameter!.Ordinal);
        var validateSource = fixture.Root("Validate");
        Assert.Single(validateSource.Body!.Statements);
        var helperSyntax = Assert.Single(validateSource.DescendantNodes().OfType<InvocationExpressionSyntax>());
        var helper = Assert.IsAssignableFrom<IInvocationOperation>(model.GetOperation(helperSyntax));
        Assert.True(KnownFrameworkExceptionModel.IsArgumentNullThrowIfNull(helper.TargetMethod, fixture.Compilation));
        var helperArgument = Assert.Single(helper.Arguments, a => a.Parameter!.Ordinal == 0).Value;
        while (helperArgument is IConversionOperation conversion)
        {
            Assert.False(conversion.Conversion.IsUserDefined);
            helperArgument = conversion.Operand;
        }
        var consumed = Assert.IsAssignableFrom<IParameterReferenceOperation>(helperArgument);
        Assert.Equal(0, consumed.Parameter.Ordinal);
        Assert.True(SymbolEqualityComparer.Default.Equals(consumed.Parameter.ContainingSymbol, fixture.Symbol("Validate")));
        Assert.Empty(validateSource.DescendantNodes().OfType<TryStatementSyntax>());
        Capture("original-static-links", new { SourceSha256 = Hash(source),
            Root = ExceptionFlowFiniteOrbitAdmission.BodyId(fixture.Symbol("M")),
            FieldOwner = owner.ToDisplayString(), AllocatedType = allocated.ToDisplayString(),
            AllocationRule = "A normal finite allocation alternative, not universal NoThrow or an initialized-state fact.",
            AllSourceBodiesAndOwnersBound = true, TableConstructorHasNoUserEffects = true,
            KeyConstructorHasNoUserEffects = true, FactoryNotInvokedByExactGetValueBody = true,
            ExactReturn = returnType.ToDisplayString(), DefaultOfClosedReferenceTypeIsNull = true,
            SameLocalPassedToValidateParameterZero = true, SameParameterConsumedByExactFrameworkGuard = true,
            NoCatchOrFinallyBetweenWitnessAndRoot = true, CurrentProductionReceiptExists = false });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SeparateSuccessAndNullPossibilitiesHaveNoCompatibleJointExecution(bool failure)
    {
        string source = Correlated(failure);
        var fixture = Fixture(source);
        var syntax = fixture.Compilation.SyntaxTrees.Single().GetRoot();
        var constructor = Assert.Single(syntax.DescendantNodes().OfType<ConstructorDeclarationSyntax>());
        var current = Assert.Single(syntax.DescendantNodes().OfType<VariableDeclaratorSyntax>(), v => v.Identifier.ValueText == "current");
        var model = fixture.Compilation.GetSemanticModel(current.SyntaxTree);
        var field = Assert.IsAssignableFrom<IFieldSymbol>(model.GetDeclaredSymbol(current));
        Assert.Equal(Accessibility.Private, field.DeclaredAccessibility);
        Assert.True(field.IsStatic);
        Assert.Equal(2, syntax.DescendantNodes().OfType<AssignmentExpressionSyntax>().Count(a =>
            SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(a.Left).Symbol, field)));
        var branch = Assert.Single(constructor.Body!.Statements.OfType<IfStatementSyntax>());
        var failed = Assert.IsType<BlockSyntax>(branch.Statement);
        Assert.Single(failed.DescendantNodes().OfType<ThrowStatementSyntax>());
        var assignment = Assert.Single(failed.Statements.OfType<ExpressionStatementSyntax>(), s => s.Expression is AssignmentExpressionSyntax a && a.Left.ToString() == "current");
        Assert.True(((AssignmentExpressionSyntax)assignment.Expression).Right.IsKind(SyntaxKind.NullLiteralExpression));
        var normal = Assert.Single(constructor.Body.Statements.OfType<ExpressionStatementSyntax>(), s => s.Expression is AssignmentExpressionSyntax a && a.Left.ToString() == "current");
        Assert.IsType<ImplicitObjectCreationExpressionSyntax>(((AssignmentExpressionSyntax)normal.Expression).Right);
        // Exhaustive for this checked, single Boolean branch. A joint row is required.
        var relation = new[] { new { Flag = true, Normal = false, ArgumentNull = true }, new { Flag = false, Normal = true, ArgumentNull = false } };
        Assert.True(relation.Any(r => r.Normal) && relation.Any(r => r.ArgumentNull));
        Assert.DoesNotContain(relation, r => r.Normal && r.ArgumentNull);
        var observed = Execute(fixture, "Run", probe: true);
        Assert.Equal(failure ? "System.TypeInitializationException" : null, observed.Outer);
        Assert.Equal(failure ? "System.InvalidOperationException" : null, observed.Inner);
        Assert.Equal(failure ? "False,True,False" : "True,False,True", observed.Probe);
        Capture("incompatible-joint-row-" + failure, new { Source = source, Relation = relation, observed,
            StaticRelationSubject = "Consumer.Read with unconstrained Input.Flag before initialization",
            RuntimeRoot = "Harness.Run", RuntimeSelectedFlag = failure,
            StateIsPrivateAndOnlyWrittenInTheTwoInitializerBranches = true,
            NullOnFailureIsNotAReachableGuardArgument = true });
    }

    [Theory]
    [InlineData("NullRoot", true)]
    [InlineData("ValueRoot", false)]
    public void OwnerScopedGenericFramesDistinguishNullDefaultFromBoxedValue(string rootName, bool expectedNull)
    {
        const string source = "#nullable enable\npublic sealed class Cache<T> { public T? GetValue() => default; } " +
            "public static class Consumer<T> { static readonly Cache<T> cache = new(); public static void Read() { var value = cache.GetValue(); Validate(value); } " +
            "static void Validate(object? value) { System.ArgumentNullException.ThrowIfNull(value); } } " +
            "public static class Harness { public static void NullRoot() => Consumer<string>.Read(); public static void ValueRoot() => Consumer<int>.Read(); }";
        var fixture = Fixture(source);
        var model = fixture.Compilation.GetSemanticModel(fixture.Root(rootName).SyntaxTree);
        var invocation = (IMethodSymbol)model.GetSymbolInfo(Assert.Single(fixture.Root(rootName).DescendantNodes().OfType<InvocationExpressionSyntax>())).Symbol!;
        var read = fixture.Root("Read");
        var selected = (IMethodSymbol)model.GetSymbolInfo(Assert.Single(read.DescendantNodes().OfType<InvocationExpressionSyntax>(), n => n.Expression is MemberAccessExpressionSyntax)).Symbol!;
        var firstFrame = new ExceptionFlowInvocationSubstitution(selected, fixture.Compilation);
        var secondFrame = new ExceptionFlowInvocationSubstitution(invocation, fixture.Compilation);
        var returnVariable = selected.OriginalDefinition.ReturnType;
        var intermediate = firstFrame.Apply(returnVariable);
        var closed = secondFrame.Apply(intermediate);
        Assert.IsAssignableFrom<ITypeParameterSymbol>(intermediate);
        Assert.NotEqual(ExceptionFlowInvocationSubstitution.Binder((ITypeParameterSymbol)returnVariable),
            ExceptionFlowInvocationSubstitution.Binder((ITypeParameterSymbol)intermediate));
        Assert.Equal(expectedNull, closed.IsReferenceType);
        Assert.Equal(expectedNull ? SpecialType.System_String : SpecialType.System_Int32, closed.SpecialType);
        var unknown = new ExceptionFlowCallContext(fixture.Symbol("Validate"));
        var nonNull = new ExceptionFlowCallContext(fixture.Symbol("Validate"), [new KeyValuePair<int, ExceptionFlowValueFacts>(0, ExceptionFlowValueFacts.NonNull)]);
        Assert.Equal(ExceptionFlowValueFacts.None, unknown.GetParameterFacts(0));
        Assert.Equal(ExceptionFlowValueFacts.NonNull, nonNull.GetParameterFacts(0));
        Assert.NotEqual(unknown.Key, nonNull.Key);
        var observed = Execute(fixture, rootName);
        Assert.Equal(expectedNull ? "System.ArgumentNullException" : null, observed.Outer);
        Capture("generic-frame-" + rootName, new { Source = source, Root = rootName,
            FirstBinder = ExceptionFlowInvocationSubstitution.Binder((ITypeParameterSymbol)returnVariable),
            IntermediateBinder = ExceptionFlowInvocationSubstitution.Binder((ITypeParameterSymbol)intermediate),
            ClosedReturn = closed.ToDisplayString(), DefaultIsNull = expectedNull, observed,
            UnknownIsNotNullProof = true, NoNewContextIdentity = true });
    }

    [Fact]
    public void MissingInitializerBodyCannotEstablishANormalContinuation()
    {
        const string source = "public static class Harness { static readonly object cache = Build(); static extern object Build(); public static void Run() { _ = cache; System.ArgumentNullException.ThrowIfNull((object?)null); } }";
        var fixture = Fixture(source);
        var run = fixture.Session();
        var result = run.Session.Analyze(fixture.Root("Run"));
        var method = fixture.Symbol("Build");
        Assert.True(method.IsExtern);
        Assert.Null(fixture.Root("Build").Body);
        Assert.False(result.ProfileOutcome!.Complete);
        Assert.Empty(result.ProfileDiagnostics!.ProvenLowerWitnesses);
        Capture("unknown-init-continuation", new { Source = source, Flow = AdmissionCharacterization.Flow(result),
            InitializerMethodHasNoBody = true, NormalContinuationNotEstablished = true,
            NoRuntimeCallToUnknownImplementation = true });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ParameterizedGuardCatchAndReplayKeepTheGapAndRebuildRootProjection(bool catches)
    {
        string call = "Validate((object?)null);";
        if (catches) call = "try { " + call + " } catch (System.ArgumentNullException) { }";
        string source = "public static class Harness { static readonly object cache = new(); public static void Run() { _ = cache; " + call +
            " } static void Validate(object? value) { System.ArgumentNullException.ThrowIfNull(value); } } public static class Safe { public static void Independent() {} }";
        var fixture = Fixture(source);
        var view = fixture.Session();
        int producers = 0;
        var session = new ExceptionFlowSummaryAnalysisSession(view.Environment, symbol => { if (symbol.Name == "Run") producers++; });
        var cold = session.Analyze(fixture.Root("Run"));
        var independent = session.Analyze(fixture.Root("Independent"));
        var warm = session.Analyze(fixture.Root("Run"));
        Assert.Equal(1, producers);
        Assert.False(cold.ProfileOutcome!.Complete);
        Assert.True(independent.ProfileOutcome!.Complete);
        Assert.Equal(JsonSerializer.Serialize(AdmissionCharacterization.Flow(cold)), JsonSerializer.Serialize(AdmissionCharacterization.Flow(warm)));
        var local = new ExceptionFlowSummaryFragment();
        var recorder = new AdmissionObservationRecorder();
        Assert.True(ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(fixture.Symbol("Run"), view.Environment, recorder, local, new(fixture.Symbol("Run"))));
        Assert.NotEmpty(local.StaticInitializationGaps);
        var edge = Assert.Single(local.CallEdges, e => e.Target.Symbol.Name == "Validate");
        Assert.Equal(catches ? 1 : 0, edge.CaughtExceptionTypes.Count);
        var observed = Execute(fixture, "Run");
        Assert.Equal(catches ? null : "System.ArgumentNullException", observed.Outer);
        Capture("parameter-catch-replay-" + catches, new { Source = source, Cold = AdmissionCharacterization.Flow(cold),
            Warm = AdmissionCharacterization.Flow(warm), Independent = AdmissionCharacterization.Flow(independent),
            producers, Events = recorder.Events, FinalFragment = AdmissionCharacterization.Fragment(local),
            Gaps = local.StaticInitializationGaps.Order(StringComparer.Ordinal).ToArray(), observed });
    }

    private static string Correlated(bool failure) => "#nullable enable\npublic static class Input { public static bool Flag; } " +
        "public static class Probe { public static bool Returned; public static bool ArgumentNull; public static bool Validated; public static string Snapshot() => Returned + \",\" + ArgumentNull + \",\" + Validated; } " +
        "public static class Consumer { private static object? current; static Consumer() { if (Input.Flag) { current = null; Probe.ArgumentNull = true; throw new System.InvalidOperationException(); } " +
        "current = new(); Probe.Returned = true; } public static void Read() { Probe.Validated = true; Validate(current); } static void Validate(object? value) { System.ArgumentNullException.ThrowIfNull(value); } } " +
        "public static class Harness { public static void Run() { Input.Flag = " + (failure ? "true" : "false") + "; Consumer.Read(); } }";

    private static string Original()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "XMLDocNormalizer.sln"))) directory = directory.Parent;
        Assert.NotNull(directory);
        var syntax = CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(directory!.FullName,
            "Tests/XMLDocNormalizerTests/Check/Semantic/Exceptions/DOC611_ConditionalWeakTableReturnFactsTests.cs")));
        var method = Assert.Single(syntax.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>(), m => m.Identifier.ValueText == "UserDefinedConditionalWeakTable_DoesNotReceiveFrameworkFact");
        return Assert.IsType<LiteralExpressionSyntax>(Assert.Single(method.DescendantNodes().OfType<VariableDeclaratorSyntax>(), v => v.Identifier.ValueText == "source").Initializer!.Value).Token.ValueText;
    }

    private static AdmissionCharacterization.Fixture Fixture(string source) => new([("InMemory.cs", source)], parse: CSharpParseOptions.Default, assemblyName: "ExceptionFlowAnalyzerTests");
    private sealed record RuntimeObservation(string? Outer, string? Inner, string? Probe);
    private static RuntimeObservation Execute(AdmissionCharacterization.Fixture fixture, string method, bool probe = false)
    {
        using var image = new MemoryStream();
        var emitted = fixture.Compilation.Emit(image);
        Assert.True(emitted.Success, string.Join("\n", emitted.Diagnostics));
        image.Position = 0;
        var world = new AssemblyLoadContext("C3G-" + Guid.NewGuid(), isCollectible: true);
        try
        {
            var assembly = world.LoadFromStream(image);
            var action = assembly.GetType("Harness", true)!.GetMethod(method, BindingFlags.Public | BindingFlags.Static)!.CreateDelegate<Action>();
            System.Exception? failure = null;
            try { action(); } catch (System.Exception exception) { failure = exception; }
            string? observed = probe ? assembly.GetType("Probe", true)!.GetMethod("Snapshot")!.CreateDelegate<Func<string>>()() : null;
            return new(failure?.GetType().FullName, failure?.InnerException?.GetType().FullName, observed);
        }
        finally { world.Unload(); }
    }
    private static string Hash(string source) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    private void Capture(string name, object evidence) => output.WriteLine("C3G_CAPTURE " + JsonSerializer.Serialize(new { Case = name, Evidence = evidence }));
}
