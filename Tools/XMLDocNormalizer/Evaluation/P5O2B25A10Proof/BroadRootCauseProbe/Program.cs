using System.Collections;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Diagnostic observer only: no test methods/assertions, Worker processes, or source programs executed.
string tool = Path.GetFullPath(args[0]), snapshot = Path.GetFullPath(args[1]), output = Path.GetFullPath(args[2]);
if (File.Exists(output)) throw new InvalidOperationException("Preserve previous observer output.");
string bin = Path.Combine(snapshot, "Tests/XMLDocNormalizerTests/bin/Debug/net8.0");
AssemblyLoadContext.Default.Resolving += (_, name) => File.Exists(Path.Combine(bin, name.Name + ".dll"))
    ? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(bin, name.Name + ".dll")) : null;
var tests = Assembly.LoadFrom(Path.Combine(bin, "XMLDocNormalizerTests.dll"));
var main = Assembly.LoadFrom(Path.Combine(bin, "XMLDocNormalizer.dll"));
const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
object? Prop(object value, string name) => value.GetType().GetProperty(name, all)?.GetValue(value);
object? Call(Type owner, string name, params object?[] values) => owner.GetMethods(all)
    .Single(m => m.Name == name && m.GetParameters().Length == values.Length).Invoke(null, values);
Type Main(string shortName) => main.GetTypes().Single(t => t.Name == shortName);
Type Helper(string shortName) => tests.GetTypes().Single(t => t.Name == shortName);
string Hash(string file) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
object Flow(object result) => new {
    Outcome = Prop(result, "ProfileOutcome"),
    Thrown = ((IEnumerable)Prop(result, "ThrownExceptions")!).Cast<ITypeSymbol>().Select(t => t.ToDisplayString()).ToArray(),
    External = ((IEnumerable)Prop(result, "ExternalDocumentationEvidenceExceptions")!).Cast<ITypeSymbol>().Select(t => t.ToDisplayString()).ToArray(),
    Uncertain = ((IEnumerable)Prop(result, "UncertainTargets")!).Cast<string>().ToArray(),
    Diagnostic = Prop(result,"ProfileDiagnostics") is object diagnostic ? new {
        ProvenLower = ((IEnumerable)Prop(diagnostic,"ProvenLowerWitnesses")!).Cast<ITypeSymbol>().Select(t=>Call(Main("RoslynCanonicalIdentityFactory"),"CreateTypeIdentity",t)).ToArray(),
        ExternalDocumentation = ((IEnumerable)Prop(diagnostic,"ExternalDocumentation")!).Cast<ITypeSymbol>().Select(t=>Call(Main("RoslynCanonicalIdentityFactory"),"CreateTypeIdentity",t)).ToArray(),
        HasEscapingUncertainty=Prop(diagnostic,"HasEscapingUncertainty"), Proof=Prop(diagnostic,"Proof"),
        NormalFindingAdmissionBlocked=Prop(diagnostic,"NormalFindingAdmissionBlocked")
    } : null
};
using var failures = JsonDocument.Parse(File.ReadAllText(Path.Combine(tool, "Evaluation/P5O2B25A10Proof/BroadFollowup/verification.json")));
var selected = failures.RootElement.GetProperty("Failures").EnumerateArray().Select(f =>
    Regex.Match(f.GetProperty("Test").GetString()!, @"([^.]+Tests)\.(\w+)(?=\(|$)").Value).ToHashSet(StringComparer.Ordinal);
using var fixtures = JsonDocument.Parse(File.ReadAllText(Path.Combine(tool, "Evaluation/P5O2B25A10Proof/Complete-fixture-preflight.json")));
var results = new List<object>();
var defaultRefs = ((IEnumerable)Helper("MetadataReferences").GetField("Default", all)?.GetValue(null)!
    ?? (IEnumerable)Helper("MetadataReferences").GetProperty("Default", all)!.GetValue(null)!).Cast<MetadataReference>().ToArray();
if (args.Length > 3 && args[3] == "structural")
{
    var dependencyType = tests.GetTypes().Single(t => t.Name == "ExceptionFlowSummaryOrchestrationDependencyTests");
    var dependencies = (IDictionary)dependencyType.GetMethod("BuildDependencies", all)!.Invoke(null, null)!;
    string[]? PathTo(Type root, Type target)
    {
        Queue<Type[]> queue = new(); queue.Enqueue([root]); HashSet<Type> seen = [];
        while (queue.TryDequeue(out var path))
        {
            Type last = path[^1]; if (!seen.Add(last)) continue;
            if (last == target) return path.Select(t => t.Name).ToArray();
            if (dependencies.Contains(last)) foreach (Type child in (IEnumerable)dependencies[last]!) queue.Enqueue([..path,child]);
        }
        return null;
    }
    var paths = new[] { "ExceptionFlowSummaryGraphBuilder", "ExceptionFlowCatchSemantics", "ExceptionFlowContextualFactEvaluator", "ExceptionFlowDelegateTargetResolver" }
        .Select(name => new { Root=name, Path=PathTo(Main(name),Main("ExceptionFlowAnalyzer")) }).ToArray();
    const string dependencySource = "using System; namespace Dependency { public static class Extensions { public static void Validate(this object? value) { ArgumentNullException.ThrowIfNull(value); } } }";
    const string consumerSource = "using Dependency; public static class Consumer { public static void M() { new object().Validate(); } }";
    string? helperFailure=null;
    try { Call(Helper("ExceptionFlowSummaryGraphProjectTestHelper"),"BuildWithSupportingSource",dependencySource,consumerSource,"M"); }
    catch(Exception ex) { helperFailure=(ex.InnerException??ex).ToString(); }
    var depTree = CSharpSyntaxTree.ParseText(dependencySource, path:"Dependency.cs");
    var dep = CSharpCompilation.Create("ExceptionFlowSummaryGraphDependency",[depTree],defaultRefs,new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,nullableContextOptions:NullableContextOptions.Enable));
    using var pe = new MemoryStream(); if (!dep.Emit(pe).Success) throw new InvalidOperationException("Diagnostic dependency did not compile.");
    var reference = MetadataReference.CreateFromImage(pe.ToArray());
    var tree = CSharpSyntaxTree.ParseText(consumerSource,path:"InMemory.cs");
    var comp = CSharpCompilation.Create("ExceptionFlowSummaryGraphConsumer",[tree],defaultRefs.Concat([reference]),new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,nullableContextOptions:NullableContextOptions.Enable));
    var rootMethod=tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
    var rootSymbol=comp.GetSemanticModel(tree).GetDeclaredSymbol(rootMethod)!;
    var reduced=(IMethodSymbol)comp.GetSemanticModel(tree).GetSymbolInfo(tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single()).Symbol!;
    var sourceMethod=dep.GetTypeByMetadataName("Dependency.Extensions")!.GetMembers("Validate").OfType<IMethodSymbol>().Single();
    var emptyFrame=Activator.CreateInstance(Main("ExceptionFlowInvocationSubstitution"),all,null,[rootSymbol,comp],null)!;
    var symbolChecks=new List<object>();
    object Canonical(ISymbol symbol) => Main("RoslynCanonicalIdentityFactory").GetMethods(all).Single(m=>m.Name=="CreateCallableIdentity"&&m.GetParameters().Length==2&&m.GetParameters()[0].ParameterType==typeof(ISymbol)).Invoke(null,[symbol,false])!;
    foreach(var item in new[] { ("reduced",reduced),("unreduced",reduced.ReducedFrom!),("source",sourceMethod) })
    {
        string? applyError=null; ISymbol? applied=null;
        try { applied=(ISymbol)emptyFrame.GetType().GetMethod("ApplyCallable",all)!.Invoke(emptyFrame,[item.Item2])!; } catch(Exception ex) {applyError=(ex.InnerException??ex).Message;}
        symbolChecks.Add(new {Variant=item.Item1,Kind=item.Item2.MethodKind.ToString(),MetadataOwner=item.Item2.ContainingAssembly.Name,
            MethodArguments=item.Item2.TypeArguments.Select(t=>t.ToDisplayString()).ToArray(),ContainingArguments=item.Item2.ContainingType.TypeArguments.Select(t=>t.ToDisplayString()).ToArray(),
            NestedOwner=item.Item2.ContainingType.ContainingType?.ToDisplayString(),Canonical=Canonical(item.Item2),
            OriginalDefinitionMemberMatches=item.Item2.ContainingType.GetMembers(item.Item2.Name).Count(c=>SymbolEqualityComparer.Default.Equals(c.OriginalDefinition,item.Item2.OriginalDefinition)),
            Applied=applied==null?null:Canonical(applied),ApplyFailure=applyError});
    }
    var context = Call(Main("ProjectClosureSemanticContext"),"CreateSingleCompilationContext",tree,comp)!;
    context.GetType().GetMethod("RegisterSupportingSource",all)!.Invoke(context,[dep]);
    var environment = Activator.CreateInstance(Main("ExceptionFlowSemanticEnvironment"),all,null,[context],null)!;
    var session = Call(Main("ExceptionFlowSummaryAnalysisSession"),"CreateSummaryAnalysisSession",environment)!;
    object?[] buildArgs = [tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(),null,null];
    string? failure = null;
    try { session.GetType().GetMethod("TryBuildTransitiveSummaryGraph",all)!.Invoke(session,buildArgs); }
    catch (Exception ex) { failure=(ex.InnerException??ex).Message; }
    var graph = buildArgs[1]!; // Raw construction returns its own graph, not the Session production graph.
    var building = Prop(graph,"BuildingKey");
    var buildingComp = Prop(graph,"BuildingCompilation") as Compilation;
    var buildingContext = Prop(graph,"BuildingContext");
    var checks = new List<object>();
    if (buildingComp != null && buildingContext != null && building != null)
    {
        var active = (ISymbol)Prop(building,"Symbol")!;
        var frame = Activator.CreateInstance(Main("ExceptionFlowInvocationSubstitution"),all,null,[Prop(buildingContext,"InvocationSymbol"),buildingComp],null)!;
        foreach (var syntax in active.DeclaringSyntaxReferences)
        foreach (var invocation in syntax.GetSyntax().DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var model = buildingComp.GetSemanticModel(invocation.SyntaxTree);
            if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol selectedMethod) continue;
            object?[] targetArgs = [selectedMethod,buildingComp,environment,null,null];
            var target = (IMethodSymbol)Main("ExceptionFlowAnalyzer").GetMethod("GetSummaryInvocationAnalysisTarget",all)!.Invoke(null,targetArgs)!;
            var targetContext = Main("ExceptionFlowAnalyzer").GetMethods(all).Single(m=>m.Name=="CreateInvocationCallContext"&&m.GetParameters().Length==5).Invoke(null,[invocation,selectedMethod,target,model,buildingContext])!;
            if (targetArgs[3] is IMethodSymbol sourceTarget)
                targetContext=targetContext.GetType().GetMethod("RebindCallable",all)!.Invoke(targetContext,[sourceTarget,(Func<ISymbol,ISymbol?>)(s=>s)])!;
            var symbolic=(ISymbol)Prop(targetContext,"InvocationSymbol")!;
            var containing=(INamedTypeSymbol)frame.GetType().GetMethod("Apply",all)!.Invoke(frame,[symbolic.ContainingType])!;
            var candidates=containing.GetMembers(symbolic.Name);
            string? applyFailure=null;
            try { frame.GetType().GetMethod("ApplyCallable",all)!.Invoke(frame,[symbolic]); } catch(Exception ex) {applyFailure=(ex.InnerException??ex).Message;}
            checks.Add(new { Invocation=invocation.ToString(),SelectedKind=selectedMethod.MethodKind.ToString(),AnalysisTarget=target.ToDisplayString(),
                SupportingSource=targetArgs[3] is IMethodSymbol ss?ss.ToDisplayString():null,Symbolic=symbolic.ToDisplayString(),SymbolicKind=(symbolic as IMethodSymbol)?.MethodKind.ToString(),
                FrameAssembly=buildingComp.AssemblyName,SymbolAssembly=symbolic.ContainingAssembly.Name,ContainingAssembly=containing.ContainingAssembly.Name,
                CandidateCount=candidates.Length,OriginalDefinitionMatches=candidates.Count(c=>SymbolEqualityComparer.Default.Equals(c.OriginalDefinition,symbolic.OriginalDefinition)),ApplyFailure=applyFailure });
        }
    }
    var malformedTree=CSharpSyntaxTree.ParseText("/// <summary>Incomplete.</summary>\r\nclass",path:"/incomplete.cs");
    var malformed=CSharpCompilation.Create("Incomplete",[malformedTree],[MetadataReference.CreateFromFile(typeof(Exception).Assembly.Location)],new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    var malformedContext=Call(Main("ProjectClosureSemanticContext"),"CreateSingleCompilationContext",malformedTree,malformed)!;
    var malformedEnv=Activator.CreateInstance(Main("ExceptionFlowSemanticEnvironment"),all,null,[malformedContext],null)!;
    var malformedSession=Call(Main("ExceptionFlowSummaryAnalysisSession"),"CreateSummaryAnalysisSession",malformedEnv)!;
    var incompleteMember=malformedTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
    var incompleteSymbol=malformed.GetSemanticModel(malformedTree).GetDeclaredSymbol(incompleteMember);
    string? incompleteError=null;
    try { malformedSession.GetType().GetMethod("Analyze",all)!.Invoke(malformedSession,[incompleteMember]); } catch(Exception ex) {incompleteError=(ex.InnerException??ex).ToString();}
    File.WriteAllText(output,JsonSerializer.Serialize(new { Kind="Read-only dependency/rebinding/incomplete-syntax trace",Paths=paths,
        ExistingHelperFailure=helperFailure,RawFailure=failure,RawGraphCount=Prop(graph,"Count"),ActiveSymbol=building==null?null:Prop(building,"Symbol")?.ToString(),Checks=checks,ReducedSymbolChecks=symbolChecks,
        SourceAndUnreducedFullCanonicalIdentityEqual=Canonical(sourceMethod).Equals(Canonical(reduced.ReducedFrom!)),
        Incomplete=new { SymbolPresent=incompleteSymbol!=null,Name=incompleteSymbol?.Name,MetadataName=incompleteSymbol?.MetadataName,IdentifierMissing=incompleteMember.Identifier.IsMissing,
            Diagnostics=malformed.GetDiagnostics().Select(d=>d.Id).ToArray(),Error=incompleteError }},new JsonSerializerOptions {WriteIndented=true}));
    Console.WriteLine(output); return;
}
foreach (var fixture in fixtures.RootElement.GetProperty("SemanticCases").EnumerateArray())
{
    string test = fixture.GetProperty("Fixture").GetProperty("Test").GetString()!;
    if (!selected.Contains(test)) continue;
    if (args.Length > 3 && args[3] == "diagnostic-side" && !test.StartsWith("ExceptionFlowExternalDocumentationTests.",StringComparison.Ordinal)
        && !test.Contains("ForeachElementFromOfTypeHelper",StringComparison.Ordinal)) continue;
    int callIndex = 0;
    foreach (var call in fixture.GetProperty("Calls").EnumerateArray())
    {
        string source = call.GetProperty("Source").GetString()!, entry = call.GetProperty("Entry").GetString()!, mode = call.GetProperty("Mode").GetString()!;
        var observations = new List<object>();
        string? error = null;
        object? native = null, findingIds = null;
        try
        {
            var extra = new List<MetadataReference>();
            foreach (var reference in call.GetProperty("ReferenceDescriptors").EnumerateArray())
            {
                if (reference.TryGetProperty("Source", out var refSource) && refSource.ValueKind == JsonValueKind.String)
                    extra.Add((MetadataReference)Call(Helper("ExternalDocumentationReferenceTestHelper"), "Create", refSource.GetString())!);
                else
                {
                    string path = reference.GetProperty("Path").GetString()!;
                    string local = Path.Combine(bin, Path.GetFileName(path));
                    extra.Add(MetadataReference.CreateFromFile(File.Exists(local) ? local : path));
                }
            }
            if (entry.StartsWith("Analyze", StringComparison.Ordinal))
            {
                string root = call.GetProperty("RootMethod").GetString()!;
                var run = Call(Helper("ExceptionFlowAnalyzerTestHelper"), entry, source, root, extra.ToArray())!;
                native = Flow(Prop(run, "Result")!);
            }
            else if (entry == "FindSemanticExceptionFindingsForSource")
            {
                var find = Helper("CheckAssert").GetMethods(all).Single(m => m.Name == entry && m.GetParameters().Length == 3);
                var found = (IEnumerable)find.Invoke(null, [source, Enum.Parse(find.GetParameters()[1].ParameterType, mode), extra.ToArray()])!;
                findingIds = found.Cast<object>().Select(f => new { Id = Prop(Prop(f, "Smell")!, "ID"), Message = Prop(f, "Message") }).ToArray();
            }
            var tree = CSharpSyntaxTree.ParseText(source, path: "InMemory.cs");
            var compilation = CSharpCompilation.Create(entry.StartsWith("Analyze", StringComparison.Ordinal) ? "ExceptionFlowAnalyzerTests" : "InMemoryAssembly",
                [tree], defaultRefs.Concat(extra), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    nullableContextOptions: entry.StartsWith("Analyze", StringComparison.Ordinal) ? NullableContextOptions.Enable : NullableContextOptions.Disable));
            string? requested = call.TryGetProperty("RootMethod", out var r) && r.ValueKind == JsonValueKind.String ? r.GetString() : null;
            var roots = tree.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>().Where(m => requested != null
                ? m is MethodDeclarationSyntax method && method.Identifier.ValueText == requested
                : m is MethodDeclarationSyntax or PropertyDeclarationSyntax or ConstructorDeclarationSyntax
                    && m.GetLeadingTrivia().ToFullString().Contains("<summary>", StringComparison.Ordinal)).ToArray();
            foreach (var member in roots)
            {
                var context = Call(Main("ProjectClosureSemanticContext"), "CreateSingleCompilationContext", tree, compilation)!;
                var environment = Activator.CreateInstance(Main("ExceptionFlowSemanticEnvironment"), all, null, [context], null)!;
                var session = Call(Main("ExceptionFlowSummaryAnalysisSession"), "CreateSummaryAnalysisSession", environment)!;
                var result = session.GetType().GetMethod("Analyze", all)!.Invoke(session, [member])!;
                observations.Add(new { Member = member is MethodDeclarationSyntax method ? method.Identifier.ValueText : member.Kind().ToString(), Result = Flow(result) });
            }
            // A recursive/raw helper may intentionally have no production profile. Keep its exact actual output separate.
        }
        catch (Exception ex) { while (ex is TargetInvocationException && ex.InnerException != null) ex = ex.InnerException; error = ex.GetType().Name + ": " + ex.Message; }
        results.Add(new { Test=test, Case=fixture.GetProperty("Fixture").GetProperty("Id").GetString(), CallIndex=callIndex++, Entry=entry, Mode=mode,
            SourceSha256=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(source))), Native=native, Findings=findingIds, Production=observations, Error=error });
    }
    Console.WriteLine(test);
}
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, JsonSerializer.Serialize(new {
    Kind="Read-only fixture/native/admission observation, not a unit or closure gate", Snapshot=snapshot,
    MainSha256=Hash(Path.Combine(bin,"XMLDocNormalizer.dll")), TestSha256=Hash(Path.Combine(bin,"XMLDocNormalizerTests.dll")),
    ExistingTestsExecuted=false, ProductSourcesChanged=false, Results=results
}, new JsonSerializerOptions { WriteIndented=true, IncludeFields=true }));
Console.WriteLine($"Observed {results.Count} calls; output {output}");
