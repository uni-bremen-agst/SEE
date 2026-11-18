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

namespace XMLDocNormalizerTests.Worker;

public sealed class ExceptionFlowAssemblyInventorySynchronizationTests(ITestOutputHelper output)
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly string[] Names = ["unchanged", "existing-add", "add", "remove", "remove-missing", "clear", "clear-sentinel",
        "replace-same-count", "refill-same-count", "union", "except", "intersect", "symmetric", "remove-where", "trim", "capacity", "null"];

    public static IEnumerable<object[]> Mutations() => Names.Select(name => new object[] { name });

    private static void Mutate(HashSet<string> set, string name, string required)
    {
        switch (name)
        {
            case "unchanged": break;
            case "existing-add": set.Add(required); break;
            case "add": set.Add("additional"); break;
            case "remove": set.Remove(required); break;
            case "remove-missing": set.Remove("not present"); break;
            case "clear": set.Clear(); break;
            case "clear-sentinel": set.Clear(); set.Add("sentinel"); break;
            case "replace-same-count": set.Remove(required); set.Add("replacement"); break;
            case "refill-same-count":
                int count = set.Count;
                set.Clear();
                for (int index = 0; index < count; index++)
                {
                    set.Add("replacement" + index);
                }
                break;
            case "union": set.UnionWith(["additional", required]); break;
            case "except": set.ExceptWith([required]); break;
            case "intersect": set.IntersectWith(["sentinel"]); break;
            case "symmetric": set.SymmetricExceptWith([required, "replacement"]); break;
            case "remove-where": set.RemoveWhere(value => value == required); break;
            case "trim": set.TrimExcess(); break;
            case "capacity": set.EnsureCapacity(1000); break;
            case "null": set.Add(null!); break;
            default: throw new ArgumentOutOfRangeException(nameof(name));
        }
    }

    [Theory]
    [MemberData(nameof(Mutations))]
    public void EveryMutationKeepsExactLegacyConsumerContentAndOrder(string mutation)
    {
        AdmissionCharacterization.Fixture fixture = Fixture();
        ExceptionFlowSemanticEnvironment environment = fixture.Session().Environment;
        HashSet<string> actual = new(StringComparer.Ordinal);
        HashSet<string> expected = new(StringComparer.Ordinal);
        object? receipt = Receipt();
        Sync(environment, actual, receipt);
        Sync(environment, expected, null);
        Sync(environment, actual, receipt);
        Sync(environment, expected, null);
        if (receipt != null)
        {
            Assert.NotNull(receipt.GetType().GetField("members", Hidden)!.GetValue(receipt));
        }
        string required = Keys(environment)[0];
        Mutate(actual, mutation, required);
        Mutate(expected, mutation, required);
        for (int iteration = 0; iteration < 8; iteration++)
        {
            Sync(environment, actual, receipt);
            Sync(environment, expected, null);
            Assert.Equal(expected.ToArray(), actual.ToArray());
        }
        Assert.All(Keys(environment), key => Assert.Contains(key, actual));
        if (mutation == "unchanged")
        {
            output.WriteLine("PERF09D_KEYS " + JsonSerializer.Serialize(Keys(environment).Select(key => key.Length).ToArray()));
        }
    }

    [Fact]
    public void SameCountMutationCannotHideBehindARestoredRuntimeGeneration()
    {
        ExceptionFlowSemanticEnvironment environment = Fixture().Session().Environment;
        HashSet<string> consumer = new(StringComparer.Ordinal);
        object? receipt = Receipt();
        Sync(environment, consumer, receipt);
        Sync(environment, consumer, receipt);
        string required = Keys(environment)[0];
        FieldInfo generation = typeof(HashSet<string>).GetField("_version", Hidden)!;
        int previous = (int)generation.GetValue(consumer)!;
        consumer.Remove(required);
        consumer.Add("same-count replacement");
        // Simulate generation aliasing after wraparound without billions of
        // operations. Production must not consult this private runtime field.
        generation.SetValue(consumer, previous);
        Sync(environment, consumer, receipt);
        Assert.Contains(required, consumer);
        Assert.Contains("same-count replacement", consumer);
    }

    [Fact]
    public void LargeSpareCapacityRetainsFullUnionWithoutKeepingASnapshot()
    {
        ExceptionFlowSemanticEnvironment environment = Fixture().Session().Environment;
        HashSet<string> consumer = new(StringComparer.Ordinal);
        object? receipt = Receipt();
        Sync(environment, consumer, receipt);
        Sync(environment, consumer, receipt);
        consumer.EnsureCapacity(10000);
        Sync(environment, consumer, receipt);
        Assert.All(Keys(environment), key => Assert.Contains(key, consumer));
        if (receipt != null)
        {
            Assert.Null(receipt.GetType().GetField("members", Hidden)!.GetValue(receipt));
        }
    }

    [Fact]
    public void SharedConsumerAndDistinctOwnersDoNotShareReceipts()
    {
        ExceptionFlowSemanticEnvironment first = Fixture("First").Session().Environment;
        ExceptionFlowSemanticEnvironment second = Fixture("Second").Session().Environment;
        HashSet<string> actual = new(StringComparer.Ordinal);
        HashSet<string> expected = new(StringComparer.Ordinal);
        object? a = Receipt();
        object? b = Receipt();
        foreach ((ExceptionFlowSemanticEnvironment environment, object? receipt) in new[] { (first, a), (second, b), (first, a), (second, b) })
        {
            Sync(environment, actual, receipt);
            Sync(environment, expected, null);
            Assert.Equal(expected.ToArray(), actual.ToArray());
        }
        actual.Clear(); expected.Clear();
        actual.Add("sentinel"); expected.Add("sentinel");
        Sync(first, actual, a); Sync(first, expected, null);
        Sync(second, actual, b); Sync(second, expected, null);
        Assert.Equal(expected.ToArray(), actual.ToArray());
    }

    [Fact]
    public void SupportingGrowthForcesCurrentInventoryAndPreservesEarlierConsumerKeys()
    {
        AdmissionCharacterization.Fixture fixture = Fixture();
        (ProjectClosureSemanticContext context, ExceptionFlowSemanticEnvironment environment, ExceptionFlowSummaryAnalysisSession _) = fixture.Session();
        HashSet<string> actual = new(StringComparer.Ordinal);
        HashSet<string> expected = new(StringComparer.Ordinal);
        object? receipt = Receipt();
        Sync(environment, actual, receipt); Sync(environment, expected, null);
        Sync(environment, actual, receipt);
        string[] before = Keys(environment);
        CSharpCompilation support = CSharpCompilation.Create("PERF09D.Support", [CSharpSyntaxTree.ParseText("public class Foreign {}")],
            MetadataReferences.Default, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        context.RegisterSupportingSource(support);
        Sync(environment, actual, receipt); Sync(environment, expected, null);
        Assert.NotSame(before, Keys(environment));
        Assert.Equal(expected.ToArray(), actual.ToArray());
        Assert.All(before, key => Assert.Contains(key, actual));
        Assert.All(Keys(environment), key => Assert.Contains(key, actual));
        Sync(environment, actual, receipt);
        Assert.Equal(expected.ToArray(), actual.ToArray());
    }

    [Fact]
    public void ThrowingDiscoveryDoesNotPublishAConsumerIntoTheNextRoot()
    {
        AdmissionCharacterization.Fixture fixture = new([("/PERF09D/Retry.cs",
            "class Entry { static void A() { Shared(); } static void B() { Shared(); } static void Shared() { throw new System.ArgumentException(); } }")]);
        ExceptionFlowSemanticEnvironment environment = fixture.Session().Environment;
        bool fail = true;
        ExceptionFlowSummaryAnalysisSession session = new(environment, symbol =>
        {
            if (fail && symbol.Name == "Shared")
            {
                throw new InventoryFailure();
            }
        });
        Assert.Throws<InventoryFailure>(() => session.Analyze(fixture.Root("A")));
        fail = false;
        foreach (string root in new[] { "A", "B", "A" })
        {
            object expected = AdmissionCharacterization.Flow(fixture.Session().Session.Analyze(fixture.Root(root)));
            object actual = AdmissionCharacterization.Flow(session.Analyze(fixture.Root(root)));
            Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual));
        }
    }

    [Fact]
    public void FreshBuildMutationCannotPublishAFalseConsumerReceipt()
    {
        ExceptionFlowSemanticEnvironment environment = Fixture().Session().Environment;
        HashSet<string> consumer = new(StringComparer.Ordinal);
        object? receipt = Receipt();
        IEnumerable<string> Projection(ExceptionFlowSemanticEnvironment unused)
        {
            yield return "first";
            consumer.Clear();
            yield return "last";
        }
        Sync(environment, consumer, receipt, Projection);
        Assert.Equal(new[] { "last" }, consumer);
        Sync(environment, consumer, receipt, Projection);
        Assert.Equal(new[] { "last", "first" }, consumer);
    }

    [Fact]
    public void FailedFreshBuildKeepsPrefixAndRepairsBeforeLaterReuse()
    {
        ExceptionFlowSemanticEnvironment environment = Fixture().Session().Environment;
        HashSet<string> consumer = new(["sentinel"], StringComparer.Ordinal);
        object? receipt = Receipt();
        IEnumerable<string> Failing(ExceptionFlowSemanticEnvironment unused)
        {
            yield return "prefix";
            throw new InventoryFailure();
        }
        Assert.Throws<InventoryFailure>(() => Sync(environment, consumer, receipt, Failing));
        Assert.Equal(new[] { "sentinel", "prefix" }, consumer);
        Sync(environment, consumer, receipt, _ => new[] { "prefix", "complete" });
        Assert.Equal(new[] { "sentinel", "prefix", "complete" }, consumer);
        consumer.Remove("prefix");
        Sync(environment, consumer, receipt);
        Assert.Contains("prefix", consumer);
        Assert.Contains("complete", consumer);
    }

    [Fact]
    public void ReentrantFreshProjectionCannotClaimAnotherConsumersContents()
    {
        ExceptionFlowSemanticEnvironment environment = Fixture().Session().Environment;
        HashSet<string> outer = new(StringComparer.Ordinal);
        HashSet<string> inner = new(StringComparer.Ordinal);
        object? a = Receipt();
        object? b = Receipt();
        IEnumerable<string> Projection(ExceptionFlowSemanticEnvironment unused)
        {
            yield return "outer-first";
            Sync(environment, inner, b, _ => new[] { "inner" });
            yield return "outer-last";
        }
        Sync(environment, outer, a, Projection);
        Assert.Equal(new[] { "inner" }, inner);
        Assert.Equal(new[] { "outer-first", "outer-last" }, outer);
        Sync(environment, inner, b);
        Assert.Equal(new[] { "inner", "outer-first", "outer-last" }, inner);
    }

    [Fact]
    public void NonOrdinalComparerRetainsItsFreshFailureContract()
    {
        ExceptionFlowSemanticEnvironment environment = Fixture().Session().Environment;
        ThrowingComparer comparer = new();
        HashSet<string> consumer = new(comparer);
        object? receipt = Receipt();
        Sync(environment, consumer, receipt);
        comparer.Fail = true;
        Assert.Throws<InventoryFailure>(() => Sync(environment, consumer, receipt));
    }

    [Theory]
    [InlineData("shared")]
    [InlineData("recursive")]
    [InlineData("outside")]
    public void ActualDiscoveryKeepsCompleteCanonicalRootProducts(string shape)
    {
        string source = shape switch
        {
            "shared" => "class Entry { static void A() { Shared(); } static void B() { Shared(); } static void Shared() { throw new System.ArgumentException(); } }",
            "recursive" => "class Entry { static void A() { B(); } static void B() { A(); throw new System.ArgumentException(); } }",
            "outside" => "class Entry { static void A() { B(); } static void B() { Missing(); } static extern void Missing(); }",
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };
        AdmissionCharacterization.Fixture fixture = new([("/PERF09D/Entry.cs", source)]);
        ExceptionFlowSemanticEnvironment environment = fixture.Session().Environment;
        ExceptionFlowFiniteOrbitAdmission.FactStore facts = new(environment);
        foreach (string root in new[] { "A", "B", "A" })
        {
            ExceptionFlowFiniteOrbitAdmission actual = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root(root), environment, facts);
            ExceptionFlowFiniteOrbitAdmission independent = ExceptionFlowFiniteOrbitAdmission.Discover(fixture.Root(root), fixture.Session().Environment);
            Assert.Equal(JsonSerializer.Serialize(independent.Outcome), JsonSerializer.Serialize(actual.Outcome));
            output.WriteLine("PERF09D_PARITY " + JsonSerializer.Serialize(new { shape, root, actual.Outcome.InventoryDigest, actual.Outcome.Complete, actual.Outcome.Reasons }));
        }
    }

    [Fact]
    public void ActualSynchronizationSourceReducesUnionWorkIncludingRepair()
    {
        Func<int, int, int, int[][]> run = MeteredSource();
        foreach (int count in new[] { 2, 6, 18 })
        {
            foreach (int operations in new[] { 1, 8, 32 })
            {
                foreach (int mutation in new[] { 0, 1, 2 })
                {
                    int[][] result = run(count, operations, mutation);
                    int[] legacy = result[0];
                    int[] optimized = result[1];
                    Assert.Equal(operations, legacy[0]);
                    Assert.Equal(count * operations, legacy[1]);
                    Assert.Equal(mutation == 0 ? 1 : mutation == 1 ? Math.Min(2, operations) : operations, optimized[0]);
                    Assert.Equal(optimized[0] * count, optimized[1]);
                    Assert.Equal(legacy[2], optimized[2]);
                    Assert.Equal(operations, legacy[5]);
                    Assert.Equal(operations, optimized[5]);
                    output.WriteLine("PERF09D_OPS " + JsonSerializer.Serialize(new { count, operations, mutation, legacy, optimized,
                        columns = new[] { "FullUnions", "UnionKeyHashChecks", "Insertions", "SnapshotEntries", "ReferenceComparisons", "ScopeReads" } }));
                }
            }
        }
    }

    private static Func<int, int, int, int[][]> MeteredSource()
    {
        string path = RootFile("src/XMLDocNormalizer/Checks/Infrastructure/Exception/Flow/ExceptionFlowSemanticEnvironment.cs");
        CompilationUnitSyntax syntax = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetCompilationUnitRoot();
        MethodDeclarationSyntax include = syntax.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "IncludeAssemblyInventory");
        ClassDeclarationSyntax receipt = syntax.DescendantNodes().OfType<ClassDeclarationSyntax>().Single(type => type.Identifier.ValueText == "AssemblyInventoryConsumer");
        InvocationExpressionSyntax union = include.DescendantNodes().OfType<InvocationExpressionSyntax>().Single(call => call.ToString() == "consumer.UnionWith(inventory)");
        include = include.ReplaceNode(union, SyntaxFactory.ParseExpression("Meter.Union(consumer, inventory)"));
        InvocationExpressionSyntax equal = receipt.DescendantNodes().OfType<InvocationExpressionSyntax>().Single(call => call.ToString() == "ReferenceEquals(members[index++], member)");
        receipt = receipt.ReplaceNode(equal, SyntaxFactory.ParseExpression("Meter.Equal(members[index++], member)"));
        InvocationExpressionSyntax snapshot = receipt.DescendantNodes().OfType<InvocationExpressionSyntax>().Single(call => call.ToString() == "current.ToArray()");
        receipt = receipt.ReplaceNode(snapshot, SyntaxFactory.ParseExpression("Meter.Snapshot(current)"));
        string members = (include + "\n" + receipt).Replace("ExceptionFlowSemanticScope", "object").Replace("ExceptionFlowSemanticEnvironment", "MeterEnvironment");
        string code = "using System; using System.Linq; using System.Collections.Generic; "
            + "public sealed class MeterEnvironment { private IReadOnlyList<object>? assemblyInventoryView; private string[]? assemblyInventory; private readonly object[] view = [new object()]; internal IReadOnlyList<object> GetAnalysisScopes() { Meter.Scope++; return view; } "
            + members + " public static int[][] Run(int count, int operations, int mutation) { string[] keys = Enumerable.Range(0,count).Select(i => i.ToString()).ToArray(); int[][] rows = new int[2][]; for(int mode=0;mode<2;mode++) { MeterEnvironment e = new(); e.IncludeAssemblyInventory(new(StringComparer.Ordinal), _ => keys); HashSet<string> c = new(StringComparer.Ordinal); AssemblyInventoryConsumer? r = mode==0 ? null : new(); Meter.Reset(); for(int i=0;i<operations;i++) { if((mutation==1 && i==operations/2) || mutation==2) { c.Clear(); } e.IncludeAssemblyInventory(c, _ => keys, r); if(!c.SetEquals(keys)) { throw new Exception(\"Missing required keys\"); } } rows[mode] = [Meter.Full, Meter.Hashes, Meter.Adds, Meter.Snapshots, Meter.Comparisons, Meter.Scope]; } return rows; } } "
            + "public static class Meter { public static int Full, Hashes, Adds, Snapshots, Comparisons, Scope; public static void Reset() { Full=Hashes=Adds=Snapshots=Comparisons=Scope=0; } public static void Union(HashSet<string> c, string[] keys) { Full++; foreach(string key in keys) { Hashes++; if(c.Add(key)) { Adds++; } } } public static string[] Snapshot(HashSet<string> c) { Snapshots+=c.Count; return c.ToArray(); } public static bool Equal(string left, string right) { Comparisons++; return ReferenceEquals(left,right); } }";
        CSharpCompilation compilation = CSharpCompilation.Create("PERF09D.Meter." + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12))], MetadataReferences.Default,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using MemoryStream image = new();
        Microsoft.CodeAnalysis.Emit.EmitResult emitted = compilation.Emit(image);
        Assert.True(emitted.Success, string.Join(Environment.NewLine, emitted.Diagnostics));
        MethodInfo method = Assembly.Load(image.ToArray()).GetType("MeterEnvironment")!.GetMethod("Run")!;
        return (count, operations, mutation) => (int[][])Call(() => method.Invoke(null, [count, operations, mutation]))!;
    }

    private static AdmissionCharacterization.Fixture Fixture(string owner = "Inventory") => new(
        [("/PERF09D/Entry.cs", "class Entry { static void A() {} static void B() {} }")], assemblyName: owner);

    private static object? Receipt() => typeof(ExceptionFlowSemanticEnvironment).GetNestedType("AssemblyInventoryConsumer", BindingFlags.NonPublic) is Type type
        ? Activator.CreateInstance(type, true) : null;

    private static string[] Keys(ExceptionFlowSemanticEnvironment environment)
        => (string[])typeof(ExceptionFlowSemanticEnvironment).GetField("assemblyInventory", Hidden)!.GetValue(environment)!;

    private static void Sync(ExceptionFlowSemanticEnvironment environment, HashSet<string> consumer, object? receipt,
        Func<ExceptionFlowSemanticEnvironment, IEnumerable<string>>? projection = null)
    {
        projection ??= typeof(ExceptionFlowFiniteOrbitAdmission).GetMethod("AssemblyInventory", BindingFlags.Static | BindingFlags.NonPublic)!
            .CreateDelegate<Func<ExceptionFlowSemanticEnvironment, IEnumerable<string>>>();
        MethodInfo method = typeof(ExceptionFlowSemanticEnvironment).GetMethod("IncludeAssemblyInventory", Hidden)!;
        object?[] arguments = method.GetParameters().Length == 2 ? [consumer, projection] : [consumer, projection, receipt];
        Call(() => method.Invoke(environment, arguments));
    }

    private static object? Call(Func<object?> action)
    {
        try
        {
            return action();
        }
        catch (TargetInvocationException exception) when (exception.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static string RootFile(string relative)
    {
        string directory = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(directory, "XMLDocNormalizer.sln")))
        {
            directory = Directory.GetParent(directory)?.FullName ?? throw new InvalidOperationException("Tool root missing.");
        }
        return Path.Combine(directory, relative);
    }

    private sealed class InventoryFailure : Exception
    {
    }
    private sealed class ThrowingComparer : IEqualityComparer<string>
    {
        internal bool Fail;
        public bool Equals(string? left, string? right) => StringComparer.Ordinal.Equals(left, right);
        public int GetHashCode(string value)
        {
            if (Fail)
            {
                throw new InventoryFailure();
            }
            return StringComparer.Ordinal.GetHashCode(value);
        }
    }
}
