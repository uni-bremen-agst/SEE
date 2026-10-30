using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Separates the fixed admission schema from actual invocation constructions (K1/K11).</summary>
public sealed class ExceptionFlowAdmissionSchemaContextTests
{
    [Fact]
    public void OriginalDefinitionSchemaIsStableWhileConcreteTypeAndMethodArgumentsChangeExceptions()
    {
        var fixture = new AdmissionCharacterization.Fixture([("/P2B/Generic.cs", """
            class Fault<T,U> : System.Exception {}
            class Box<T> { public static void M<U>() { throw new Fault<T,U>(); } }
            class Entry { public static void A() { Box<int>.M<string>(); } public static void B() { Box<string>.M<int>(); } }
            """)]);
        var run = fixture.Session();
        var definition = fixture.Symbol("M");
        var box = fixture.Compilation.GetTypeByMetadataName("Box`1")!;
        var integer = fixture.Compilation.GetSpecialType(SpecialType.System_Int32);
        var text = fixture.Compilation.GetSpecialType(SpecialType.System_String);
        var first = box.Construct(integer).GetMembers("M").OfType<IMethodSymbol>().Single().Construct(text);
        var second = box.Construct(text).GetMembers("M").OfType<IMethodSymbol>().Single().Construct(integer);
        Assert.True(SymbolEqualityComparer.Default.Equals(first.OriginalDefinition, second.OriginalDefinition));
        var schemas = new List<string>();
        var concrete = new List<string>();
        foreach (var invocation in new[] { first, second, first })
        {
            var schemaSink = new AdmissionObservationRecorder();
            var schema = new ExceptionFlowSummaryFragment();
            Assert.True(ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(definition, run.Environment,
                schemaSink, schema, new(definition)));
            schemas.Add(string.Join("\n", schemaSink.Events) + AdmissionCharacterization.Fragment(schema));
            var fragment = new ExceptionFlowSummaryFragment();
            Assert.True(ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(definition, run.Environment,
                new AdmissionObservationRecorder(), fragment, new(invocation)));
            concrete.Add(AdmissionCharacterization.TypeKey(Assert.Single(fragment.Sources).ExceptionType));
        }
        Assert.Equal(schemas[0], schemas[1]);
        Assert.Equal(schemas[0], schemas[2]);
        Assert.NotEqual(concrete[0], concrete[1]);
        Assert.Equal(concrete[0], concrete[2]);
        Assert.True(run.Session.Analyze(fixture.Root("A")).ProfileOutcome!.Complete);
        Assert.True(run.Session.Analyze(fixture.Root("B")).ProfileOutcome!.Complete);
    }

    [Fact]
    public void ConstructedReceiverArgumentsSelectDifferentRuntimeTargetsForTheSameDeclaration()
    {
        var fixture = new AdmissionCharacterization.Fixture([("/P2B/Dispatch.cs", """
            interface IService<T> { void Run(); }
            sealed class IntService : IService<int> { public void Run() {} }
            sealed class StringService : IService<string> { public void Run() {} }
            """)]);
        var run = fixture.Session();
        var service = fixture.Compilation.GetTypeByMetadataName("IService`1")!;
        string[] Targets(SpecialType type)
        {
            var receiver = service.Construct(fixture.Compilation.GetSpecialType(type));
            var method = receiver.GetMembers("Run").OfType<IMethodSymbol>().Single();
            return ExceptionFlowAnalyzer.ResolveSummaryRuntimeTargets(method, receiver, null, run.Environment)
                .Select(target => target.ContainingType.Name).ToArray();
        }
        Assert.Equal(new[] { "IntService" }, Targets(SpecialType.System_Int32));
        Assert.Equal(new[] { "StringService" }, Targets(SpecialType.System_String));
        Assert.Equal(new[] { "IntService" }, Targets(SpecialType.System_Int32));
    }

    [Fact]
    public void ConstructedConstructorContextsRetainTheirDifferentExceptionArguments()
    {
        var fixture = new AdmissionCharacterization.Fixture([("/P2B/Constructors.cs", """
            class Fault<T> : System.Exception {}
            class Box<T> { public Box() { throw new Fault<T>(); } }
            """)]);
        var run = fixture.Session();
        var box = fixture.Compilation.GetTypeByMetadataName("Box`1")!;
        var definition = Assert.Single(box.InstanceConstructors);
        var values = new List<string>();
        foreach (var type in new[] { SpecialType.System_Int32, SpecialType.System_String, SpecialType.System_Int32 })
        {
            var invocation = Assert.Single(box.Construct(fixture.Compilation.GetSpecialType(type)).InstanceConstructors);
            Assert.True(SymbolEqualityComparer.Default.Equals(definition, invocation.OriginalDefinition));
            var fragment = new ExceptionFlowSummaryFragment();
            Assert.True(ExceptionFlowAnalyzer.AnalyzeSummarySymbolDeclarations(definition, run.Environment,
                new AdmissionObservationRecorder(), fragment, new(invocation)));
            values.Add(AdmissionCharacterization.TypeKey(Assert.Single(fragment.Sources).ExceptionType));
        }
        Assert.NotEqual(values[0], values[1]);
        Assert.Equal(values[0], values[2]);
    }
}
