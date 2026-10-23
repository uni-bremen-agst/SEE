using System.Collections;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Additional A10 production-path checks. No raw graph, test method, submitted
// source program or alternate production facade is executed by this harness.
string snapshot = Path.GetFullPath(args[0]), output = Path.GetFullPath(args[1]);
if (File.Exists(output)) { Console.Error.WriteLine("Preserve previous attempt."); return 1; }
var checks = new List<object>();
var observations = new List<object>();
try
{
    string bin = Path.Combine(snapshot, "Tests/XMLDocNormalizerTests/bin/Debug/net8.0");
    AssemblyLoadContext.Default.Resolving += (_, name) => File.Exists(Path.Combine(bin, name.Name + ".dll"))
        ? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(bin, name.Name + ".dll")) : null;
    var main = Assembly.LoadFrom(Path.Combine(bin, "XMLDocNormalizer.dll"));
    var tests = Assembly.LoadFrom(Path.Combine(bin, "XMLDocNormalizerTests.dll"));
    const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    Type Main(string name) => main.GetTypes().Single(type => type.Name == name);
    object? Property(object instance, string name) => instance.GetType().GetProperty(name, all)!.GetValue(instance);
    object Static(string type, string name, params object[] arguments) => Main(type).GetMethods(all)
        .Single(method => method.Name == name && method.GetParameters().Length == arguments.Length).Invoke(null, arguments)!;
    var references = ((IEnumerable)tests.GetTypes().Single(type => type.Name == "MetadataReferences")
        .GetProperty("Default", all)!.GetValue(null)!).Cast<MetadataReference>().ToArray();
    void Check(string name, bool passed)
    {
        checks.Add(new { Name = name, Passed = passed });
        if (!passed) throw new InvalidOperationException("Production validation failed: " + name);
    }
    CSharpCompilation Compile(string owner, string source, params MetadataReference[] extra)
    {
        var result = CSharpCompilation.Create(owner, [CSharpSyntaxTree.ParseText(source, path: "/A10Closure/" + owner + ".cs")],
            references.Concat(extra), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Check(owner + ":valid-input", !result.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        return result;
    }
    PortableExecutableReference Emit(CSharpCompilation compilation)
    {
        using var image = new MemoryStream();
        Check(compilation.AssemblyName + ":emit-reference", compilation.Emit(image).Success);
        return MetadataReference.CreateFromImage(image.ToArray());
    }
    (object Session, object Result, int Nodes) Analyze(CSharpCompilation compilation, params CSharpCompilation[] supporting)
    {
        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Root");
        var context = Static("ProjectClosureSemanticContext", "CreateSingleCompilationContext", tree, compilation);
        foreach (var scope in supporting)
            context.GetType().GetMethods(all).Single(method => method.Name == "RegisterSupportingSource"
                && method.GetParameters().Length == 1).Invoke(context, [scope]);
        var environment = Activator.CreateInstance(Main("ExceptionFlowSemanticEnvironment"), all, null, [context], null)!;
        var session = Static("ExceptionFlowSummaryAnalysisSession", "CreateSummaryAnalysisSession", environment);
        var result = session.GetType().GetMethod("Analyze", all)!.Invoke(session, [root])!;
        var graph = session.GetType().GetField("graph", all)!.GetValue(session)!;
        int count = (int)Property(graph, "Count")!;
        observations.Add(new { Owner = compilation.AssemblyName, Outcome = Property(result, "ProfileOutcome"), Nodes = count,
            Thrown = ((IEnumerable)Property(result, "ThrownExceptions")!).Cast<INamedTypeSymbol>()
                .Select(type => Static("RoslynCanonicalIdentityFactory", "CreateTypeIdentity", type)).ToArray() });
        return (session, result, count);
    }
    INamedTypeSymbol[] Thrown(object result) => ((IEnumerable)Property(result, "ThrownExceptions")!).Cast<INamedTypeSymbol>().ToArray();
    bool Complete(object result) => (bool)Property(Property(result, "ProfileOutcome")!, "Complete")!;

    const string fault = "class Fault<T> : System.Exception {} ";
    var orbit = Analyze(Compile("Orbit", fault + "class Api { public static void Recur<T,U>() { Recur<U,T>(); throw new Fault<T>(); } } class Entry { public static void Root() { Api.Recur<int,string>(); } }"));
    Check("production-permutation-admitted", Complete(orbit.Result));
    Check("production-two-exact-constructions-distinct", orbit.Nodes == 3 && Thrown(orbit.Result).Length == 2);
    Check("production-permutation-concrete-results", Thrown(orbit.Result).Select(type => type.TypeArguments.Single().SpecialType)
        .ToHashSet().SetEquals([SpecialType.System_Int32, SpecialType.System_String]));

    var constant = Analyze(Compile("Constant", fault + "class Api { public static void Recur<T>() { Recur<int>(); throw new Fault<T>(); } } class Entry { public static void Root() { Api.Recur<string>(); } }"));
    Check("production-constant-admitted", Complete(constant.Result));
    Check("production-constant-finite-three-nodes", constant.Nodes == 3 && Thrown(constant.Result).Length == 2);

    var nested = Analyze(Compile("Nested", "class Outer<T> { public class Fault<U> : System.Exception {} public static void Throw<U>() { throw new Fault<U>(); } } class Entry { public static void Root() { try { Outer<int>.Throw<string>(); } catch (Outer<string>.Fault<string>) {} } }"));
    Check("production-method-containing-nested-admitted", Complete(nested.Result));
    var nestedFault = Thrown(nested.Result).Single();
    Check("production-nested-method-argument", nestedFault.TypeArguments.Single().SpecialType == SpecialType.System_String);
    Check("production-containing-catch-not-definition-folded", nestedFault.ContainingType.TypeArguments.Single().SpecialType == SpecialType.System_Int32);

    var inherited = Analyze(Compile("Inherited", "class Fault<T> : System.Exception {} class Derived<T> : Fault<T> {} class Api { public static void Throw<T>() { throw new Derived<T>(); } } class Entry { public static void Root() { try { Api.Throw<int>(); } catch (Fault<int>) {} Api.Throw<string>(); } }"));
    Check("production-inherited-catch-admitted", Complete(inherited.Result));
    Check("production-inherited-catch-exact-arguments", Thrown(inherited.Result).Single().TypeArguments.Single().SpecialType == SpecialType.System_String);

    var mutual = Analyze(Compile("Mutual", "class Api { public static void A<T>() { B<System.Collections.Generic.List<T>>(); } public static void B<T>() { A<T>(); } } class Entry { public static void Root() { Api.A<int>(); } }"));
    Check("production-mutual-positive-cycle-outside-before-root", !Complete(mutual.Result) && mutual.Nodes == 0 && Thrown(mutual.Result).Length == 0);

    // Real source/PE scopes A -> B -> C, not a raw graph or a display-name mock.
    var c = Compile("Closure.C", "public class Fault<T> : System.Exception {} public static class Leaf { public static void Throw<T>() { throw new Fault<T>(); } }");
    var cReference = Emit(c);
    var b = Compile("Closure.B", "public static class Middle { public static void Forward<T>() { Leaf.Throw<System.Collections.Generic.List<T>>(); } }", cReference);
    var bReference = Emit(b);
    var a = Compile("Closure.A", "class Entry { public static void Root() { try { Middle.Forward<string>(); } catch (Fault<System.Collections.Generic.List<int>>) {} } }", bReference, cReference);
    var chain = Analyze(a, b, c);
    Check("production-A-B-C-admitted", Complete(chain.Result));
    var projected = Thrown(chain.Result).Single();
    Check("production-A-B-C-full-owner", projected.ContainingAssembly.Name == "Closure.C" && projected.ContainingModule.Name == "Closure.C.dll");
    Check("production-A-B-C-composed-substitution", projected.TypeArguments.Single() is INamedTypeSymbol { Name: "List" } list
        && list.TypeArguments.Single().SpecialType == SpecialType.System_String);
    Check("production-A-B-C-finite-contexts", chain.Nodes == 3);
    var missing = Analyze(a, b);
    Check("production-A-B-C-missing-scope-fail-closed", !Complete(missing.Result) && missing.Nodes == 0 && Thrown(missing.Result).Length == 0);

    var growingC = Compile("Growth.C", "public static class Leaf { public static void Recur<T>() { Recur<System.Collections.Generic.List<T>>(); } }");
    var growingReference = Emit(growingC);
    var growingA = Compile("Growth.A", "class Entry { public static void Root() { Leaf.Recur<int>(); } }", growingReference);
    var outside = Analyze(growingA, growingC);
    Check("production-cross-scope-growth-outside-before-root", !Complete(outside.Result) && outside.Nodes == 0 && Thrown(outside.Result).Length == 0);

    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
    File.WriteAllText(output, JsonSerializer.Serialize(new { Passed = true, Checks = checks, Observations = observations,
        Entry = "ExceptionFlowSummaryAnalysisSession.Analyze", RawGraphsUsed = false, ExistingTestMethodsExecuted = false,
        SubmittedSourceExecuted = false, NewProductionFacade = false }, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"PASS: {checks.Count} additional real production-path checks; no raw-graph certification.");
    return 0;
}
catch (Exception error)
{
    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
    File.WriteAllText(output, JsonSerializer.Serialize(new { Passed = false, Checks = checks, Observations = observations,
        Error = error.ToString() }, new JsonSerializerOptions { WriteIndented = true }));
    Console.Error.WriteLine(error);
    return 1;
}
