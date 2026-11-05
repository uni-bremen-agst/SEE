using System.Text.Json;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Cli;

public sealed class CliProgressQueryLifetimeTests
{
    [Fact]
    public void ConsecutiveQueriesOnTheSameSessionNeverReuseAnEarlierObserver()
    {
        var fixture = Fixture("void A() { B(); } void B() {}", "A();");
        var run = fixture.Session();
        var first = new List<ExceptionFlowProgressEvent>(); var second = new List<ExceptionFlowProgressEvent>();
        string initial = Analyze(fixture, run, first.Add);
        int firstCount = first.Count;
        Assert.Equal(initial, Analyze(fixture, run, null));
        Assert.Equal(firstCount, first.Count);
        Assert.Equal(initial, Analyze(fixture, run, second.Add));
        Assert.Equal(firstCount, first.Count);
        Assert.Single(second, e => e.Kind == ExceptionFlowProgressKind.RootStarted);
        Assert.Single(second, e => e.Kind == ExceptionFlowProgressKind.RootCompleted);
        Assert.Contains(second, e => e.Kind == ExceptionFlowProgressKind.Admission);
        Assert.DoesNotContain(second, e => e.Kind == ExceptionFlowProgressKind.ContextDiscovered);
    }

    [Fact]
    public void IndependentSessionsHaveIndependentObserverEventsAndEquivalentResults()
    {
        var fixture = Fixture("void A() { B(); } void B() {}", "A();");
        var first = new List<ExceptionFlowProgressEvent>(); var second = new List<ExceptionFlowProgressEvent>();
        string expected = Analyze(fixture, fixture.Session(), first.Add);
        int completed = first.Count;
        Assert.Equal(expected, Analyze(fixture, fixture.Session(), second.Add));
        Assert.Equal(completed, first.Count);
        Assert.Equal(3, first.Where(e => e.Kind == ExceptionFlowProgressKind.ContextDiscovered).Sum(e => e.Count));
        Assert.Equal(3, second.Where(e => e.Kind == ExceptionFlowProgressKind.ContextDiscovered).Sum(e => e.Count));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(64)]
    public void TransitiveQueriesKeepRealProgressThroughoutTheWorkQueue(int length)
    {
        string methods = string.Join("\n", Enumerable.Range(0, length).Select(i =>
            "void M" + i + "() { " + (i + 1 < length ? "M" + (i + 1) + "();" : "") + " }"));
        var fixture = Fixture(methods, "M0();"); var run = fixture.Session();
        var events = new List<ExceptionFlowProgressEvent>();
        Analyze(fixture, run, events.Add);
        int contexts = AdmissionCharacterization.Field<ExceptionFlowSummaryGraph>(run.Session, "graph").Count;
        Assert.Equal(length + 1, contexts);
        Assert.Equal(contexts, events.Where(e => e.Kind == ExceptionFlowProgressKind.ContextDiscovered).Sum(e => e.Count));
        Assert.Equal(contexts, events.Count(e => e.Kind == ExceptionFlowProgressKind.ContextStarted));
        Assert.Equal(contexts, events.Count(e => e.Kind == ExceptionFlowProgressKind.ContextProcessed));
        Assert.Contains(events.Skip(events.FindIndex(e => e.Kind == ExceptionFlowProgressKind.ContextProcessed) + 1),
            e => e.Kind == ExceptionFlowProgressKind.ContextDiscovered);
        Assert.Single(events, e => e.Kind == ExceptionFlowProgressKind.RootCompleted);
        Assert.Contains(events, e => e.Kind == ExceptionFlowProgressKind.Evaluation);
    }

    [Fact]
    public void MultipleSuccessorsInOneBodyAreCountedAsAllDiscoveredContexts()
    {
        var fixture = Fixture("void A() {} void B() {}", "A(); B(); A();"); var run = fixture.Session();
        var events = new List<ExceptionFlowProgressEvent>(); Analyze(fixture, run, events.Add);
        Assert.Equal(3, events.Where(e => e.Kind == ExceptionFlowProgressKind.ContextDiscovered).Sum(e => e.Count));
        Assert.Contains(events, e => e.Kind == ExceptionFlowProgressKind.ContextDiscovered && e.Count == 2);
        Assert.Equal(3, events.Count(e => e.Kind == ExceptionFlowProgressKind.ContextProcessed));
    }

    [Fact]
    public void AThrowingQueryObserverCannotEscapeIntoAnUnobservedQuery()
    {
        var fixture = Fixture("void A() {}", "A();"); var run = fixture.Session();
        string expected = Analyze(fixture, fixture.Session(), null);
        Assert.Equal(expected, Analyze(fixture, run, _ => throw new IOException("closed observer")));
        Assert.Equal(expected, Analyze(fixture, run, null));
    }

    private static AdmissionCharacterization.Fixture Fixture(string methods, string body)
        => new([("/CLI1/QueryLifetime.cs", "class Fault : System.Exception {} class Entry {\n"
            + "/// <summary>Run.</summary>\n/// <exception cref=\"Fault\">Unused.</exception>\n"
            + "public void Run() { " + body + " }\n" + methods + " }")], assemblyName: "CLI1.QueryLifetime");

    private static string Analyze(AdmissionCharacterization.Fixture fixture,
        (XMLDocNormalizer.Execution.Semantic.ProjectClosureSemanticContext Context, ExceptionFlowSemanticEnvironment Environment, ExceptionFlowSummaryAnalysisSession Session) run,
        Action<ExceptionFlowProgressEvent>? observer)
    {
        var tree = fixture.Compilation.SyntaxTrees.Single(); var coverage = new AnalysisCoverage();
        coverage.Begin("QueryFixture", ExceptionAnalysisMode.SolutionTransitive);
        var findings = XmlDocExceptionSemanticDetector.FindExceptionSmells(tree, tree.FilePath,
            fixture.Compilation.GetSemanticModel(tree), run.Context,
            new XmlDocOptions { ExceptionAnalysisMode = ExceptionAnalysisMode.SolutionTransitive }, run.Session, coverage, observer);
        return JsonSerializer.Serialize(new { findings, Coverage = coverage.Report() });
    }
}
