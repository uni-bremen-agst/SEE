using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Analytical finite harness only. No XMLDocNormalizer API/algorithm is used or implemented.
internal static class FamilyProof
{
    private static readonly List<object> Checks = [];
    private const string Source = """
        using System;
        using System.Collections.Generic;
        public class End { }
        public class A<T> { }
        public class B<T> { }
        public class F<T> { }
        public class G<T> { }
        public class Pair<X,Y> { }
        public class Fault<X,Y> : Exception { }
        public class Derived<X,Y> : Fault<X,Y> { }
        public class Witness : Exception { }
        public class Outer<T> { public class Inner<U> { public static void M<V>() { } } }
        public static class Api
        {
            public static void ConstructedRoot() => Outer<int>.Inner<string>.M<bool>();
            public static void Composed(Dictionary<string,List<int>> input) { }
            public static void Root(Func<int> next)
            {
                // At least one tile is mandatory: the empty word is NOT a witness.
                switch (next())
                {
                    case 1: Search<A<End>,A<A<End>>>(next); break;
                    case 2: Search<A<A<End>>,A<End>>(next); break;
                }
            }
            public static void Search<X,Y>(Func<int> next)
            {
                switch (next())
                {
                    case 0:
                        try { Throw<X,Y>(); }
                        catch (Fault<X,X>) { throw new Witness(); }
                        catch (Exception) { }
                        break;
                    case 1: Search<A<X>,A<A<Y>>>(next); break;
                    case 2: Search<A<A<X>>,A<Y>>(next); break;
                }
            }
            private static void Throw<X,Y>() { throw new Fault<X,Y>(); }
            public static void Left<T>(Func<int> next)
            { if (next() == 0) throw new Fault<T,T>(); Right<List<T>>(next); }
            public static void Right<U>(Func<int> next) { Left<Dictionary<string,U>>(next); }
        }
        """;

    private static bool Same(ITypeSymbol x, ITypeSymbol y) => SymbolEqualityComparer.Default.Equals(x, y);
    private static void Check(string name, bool condition, object? observation = null)
    {
        if (!condition) throw new InvalidOperationException(name);
        Checks.Add(new { Name = name, Passed = true, Observation = observation });
    }

    public static void Main(string[] args)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var tree = CSharpSyntaxTree.ParseText(Source, path: "a8-legal-generic-witness.cs");
        var compilation = CSharpCompilation.Create("A8ActualTypes", [tree], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var diagnostics = compilation.GetDiagnostics();
        Check("real-csharp-compiles", !diagnostics.Any(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning),
            diagnostics.Select(d => d.ToString()).ToArray());
        INamedTypeSymbol Type(string name) => compilation.GetTypeByMetadataName(name)!;
        var end = Type("End");
        var number = compilation.GetSpecialType(SpecialType.System_Int32);
        var text = compilation.GetSpecialType(SpecialType.System_String);
        var list = Type("System.Collections.Generic.List`1");
        var dictionary = Type("System.Collections.Generic.Dictionary`2");
        var pair = Type("Pair`2");
        var fault = Type("Fault`2");
        Check("identity-formal-construction", Same(pair.Construct(pair.TypeParameters.Cast<ITypeSymbol>().ToArray()), pair));
        var composed = Type("Api").GetMembers("Composed").OfType<IMethodSymbol>().Single().Parameters.Single().Type;
        Check("composition-agrees-with-real-csharp", Same(dictionary.Construct(text, list.Construct(number)), composed));
        var current = (ITypeSymbol)number;
        var rows = new List<object>();
        for (int n = 0; n < 6; n++)
        {
            var repeated = pair.Construct(current, list.Construct(current));
            Check("correlated-pair-" + n, Same(repeated.TypeArguments[0], ((INamedTypeSymbol)repeated.TypeArguments[1]).TypeArguments[0]));
            var thrown = fault.Construct(current, current);
            var caught = fault.Construct(number, number);
            bool matches = compilation.ClassifyConversion(thrown, caught).IsImplicit;
            Check("closed-catch-partition-" + n, matches == (n == 0));
            rows.Add(new { N = n, Thrown = thrown.ToDisplayString(), Caught = matches, Remainder = !matches });
            Check("dictionary-" + n, Same(dictionary.Construct(text, current).TypeArguments[1], current));
            Check("nested-FG-" + n, Same(((INamedTypeSymbol)Type("F`1").Construct(Type("G`1").Construct(current)).TypeArguments[0]).TypeArguments[0], current));
            current = list.Construct(current);
        }
        Check("inheritance-catch", compilation.ClassifyConversion(Type("Derived`2").Construct(number, text), fault.Construct(number, text)).IsImplicit);
        Check("different-construction-not-caught", !compilation.ClassifyConversion(fault.Construct(number, text), fault.Construct(number, number)).IsImplicit);
        var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.ValueText == "ConstructedRoot");
        var selected = (IMethodSymbol)compilation.GetSemanticModel(tree).GetSymbolInfo(method.DescendantNodes().OfType<InvocationExpressionSyntax>().Single()).Symbol!;
        Check("method-containing-nested", Same(selected.TypeArguments.Single(), compilation.GetSpecialType(SpecialType.System_Boolean))
            && Same(selected.ContainingType.TypeArguments.Single(), text) && Same(selected.ContainingType.ContainingType!.TypeArguments.Single(), number));
        var search = Type("Api").GetMembers("Search").OfType<IMethodSymbol>().Single();
        var right = Type("Api").GetMembers("Right").OfType<IMethodSymbol>().Single();
        Check("formal-binder-owners", !Same(search.TypeParameters[0], search.TypeParameters[1]) && !Same(search.TypeParameters[0], right.TypeParameters[0]));
        var left = Type("Api").GetMembers("Left").OfType<IMethodSymbol>().Single();
        foreach (var symbol in new[] { left, right })
        {
            var declaration = symbol.DeclaringSyntaxReferences.Single().GetSyntax();
            var call = declaration.DescendantNodes().OfType<InvocationExpressionSyntax>().Last();
            var target = (IMethodSymbol)compilation.GetSemanticModel(tree).GetSymbolInfo(call).Symbol!;
            Check("mutual-edge-" + symbol.Name, !target.IsDefinition && target.TypeArguments.Single() is INamedTypeSymbol);
        }
        INamedTypeSymbol Encode(string word, ITypeSymbol tail)
        {
            ITypeSymbol result = tail;
            foreach (char letter in word.Reverse()) result = Type(letter == 'a' ? "A`1" : "B`1").Construct(result);
            return (INamedTypeSymbol)result;
        }
        var oneLeft = Encode("a", end); var oneRight = Encode("aa", end);
        var twoLeft = Encode("aa", oneLeft); var twoRight = Encode("a", oneRight);
        Check("tile-single-not-diagonal", !Same(oneLeft, oneRight));
        Check("tile-composed-diagonal", Same(twoLeft, twoRight));
        Check("diagonal-catch-is-equality", compilation.ClassifyConversion(fault.Construct(twoLeft, twoRight), fault.Construct(twoLeft, twoLeft)).IsImplicit);
        // All paired coordinates below are generated by ONE shared tile trace.
        var finiteTrace = new List<(ITypeSymbol X, ITypeSymbol Y)>();
        void Visit(ITypeSymbol x, ITypeSymbol y, int remaining)
        {
            if (remaining == 0) return;
            foreach (var tile in new[] { ("a", "aa"), ("aa", "a") })
            {
                var nx = Encode(tile.Item1, x); var ny = Encode(tile.Item2, y);
                finiteTrace.Add((nx, ny)); Visit(nx, ny, remaining - 1);
            }
        }
        Visit(end, end, 3); // finite illustration only, never a production decision bound.
        Check("shared-trace-prefix", finiteTrace.Count == 14 && finiteTrace.Any(p => Same(p.X, p.Y)));
        using var image = new MemoryStream();
        Check("emit-real-csharp", compilation.Emit(image).Success);
        image.Position = 0;
        var context = new AssemblyLoadContext("a8-finite-proof", isCollectible: true);
        try
        {
            var assembly = context.LoadFromStream(image);
            var root = assembly.GetType("Api")!.GetMethod("Root")!;
            bool ExecutesWitness(params int[] choices)
            {
                int index = 0;
                Func<int> next = () => choices[index++];
                try { root.Invoke(null, [next]); return false; }
                catch (TargetInvocationException exception) when (exception.InnerException?.GetType().Name == "Witness") { return true; }
            }
            Check("real-runtime-positive-catch", ExecutesWitness(1, 2, 0));
            Check("real-runtime-negative-catch", !ExecutesWitness(1, 0));
        }
        finally { context.Unload(); }
        var output = new
        {
            ContractOnly = true,
            ProductAnalyzerExecuted = false,
            ExistingTestsChanged = false,
            Checks = Checks,
            CatchPartition = rows,
            FiniteTrace = finiteTrace.Select(p => new { X = p.X.ToDisplayString(), Y = p.Y.ToDisplayString() }).ToArray(),
            GeneralUndecidabilityProvedByFiniteHarness = false,
            InfiniteRuntimeAnalysisExecuted = false,
            Compiler = typeof(CSharpCompilation).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion,
            CompilerPath = typeof(CSharpCompilation).Assembly.Location
        };
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[0]))!);
        File.WriteAllText(args[0], JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"PASS: {Checks.Count} finite Roslyn/runtime proof checks; NOT a proof of general decidability or a product implementation.");
    }
}
