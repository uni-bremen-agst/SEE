using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Checks;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizer.Configuration;
using XMLDocNormalizer.Execution.Semantic;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>Runs unchanged against the parent and B9 detector for complete finding/session parity.</summary>
    public sealed class ExceptionFlowProductionCurrentParityTests
    {
        [Theory]
        [InlineData("Direct", false)]
        [InlineData("Direct", true)]
        [InlineData("ProjectTransitive", false)]
        [InlineData("ProjectTransitive", true)]
        [InlineData("ProjectTransitiveDeclaredExceptions", false)]
        [InlineData("ProjectTransitiveDeclaredExceptions", true)]
        [InlineData("SolutionTransitive", false)]
        [InlineData("SolutionTransitive", true)]
        public void Detector_AllModesRetainFindingsAndCallerOwnedSession(string mode, bool declared)
        {
            string source = """
                public static class Fixture
                {
                    /// <summary>Calls a throwing member.</summary>
                    /// <exception cref="System.ArgumentException">Not thrown.</exception>
                    public static void Root() { Thrower(); }
                    /// <summary>Throws directly.</summary>
                    public static void Thrower() { throw null; }
                }
                """ + (declared ? "\npublic class DeclaredException : System.Exception { }\n" : "\n");
            var tree = CSharpSyntaxTree.ParseText(source, path: "production-input.cs");
            var compilation = CSharpCompilation.Create("ProductionCurrent", [tree], MetadataReferences.Default,
                new CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));
            var context = ProjectClosureSemanticContext.CreateSingleCompilationContext(tree, compilation);
            var session = ExceptionFlowSummaryAnalysisSession.CreateSummaryAnalysisSession(new(context));
            var graph = (ExceptionFlowSummaryGraph)typeof(ExceptionFlowSummaryAnalysisSession)
                .GetField("graph", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(session)!;
            var options = new XmlDocOptions { ExceptionAnalysisMode = Enum.Parse<ExceptionAnalysisMode>(mode) };
            List<Finding> Analyze() => XmlDocExceptionSemanticDetector.FindExceptionSmells(
                tree, "production-input.cs", compilation.GetSemanticModel(tree), context, options, session);
            var first = Analyze();
            string firstJson = JsonSerializer.Serialize(first);
            int count = graph.Count;
            Assert.Equal(firstJson, JsonSerializer.Serialize(Analyze()));
            Assert.Same(graph, typeof(ExceptionFlowSummaryAnalysisSession)
                .GetField("graph", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(session));
            Assert.Equal(count, graph.Count);
            if (mode == "ProjectTransitiveDeclaredExceptions") { Assert.Empty(first); }
            else { Assert.Contains(first, finding => finding.Smell.ID == (mode == "Direct" ? "DOC630" : "DOC632")); }
            if (mode == "Direct" || (mode == "ProjectTransitiveDeclaredExceptions" && !declared))
            {
                Assert.Equal(0, count);
            }
            else { Assert.True(count > 0); }
            string evidence = Path.Combine(Root(), "artifacts", "p5o2b9", "current", mode + "-" + declared + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(evidence)!);
            File.WriteAllText(evidence, JsonSerializer.Serialize(new
            {
                Mode = mode,
                Declared = declared,
                Findings = first,
                GraphCount = count,
                SessionReused = true,
                RepeatedFindingsEqual = true
            }));
        }

        internal static string Root()
        {
            for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "XMLDocNormalizer.sln"))) { return directory.FullName; }
            }
            throw new InvalidOperationException("Cannot locate tool root.");
        }
    }
}
