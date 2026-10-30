using System.Text;
using System.Security.Cryptography;
using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;
using Xunit.Abstractions;

namespace XMLDocNormalizerTests.Worker;

/// <summary>Freezes per-root products before any admission reuse exists (P2A K2/K6/K11/K14).</summary>
public sealed class ExceptionFlowAdmissionRootIsolationTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("complete")]
    [InlineData("outside")]
    [InlineData("growth")]
    [InlineData("constructed")]
    public void RootOrderOverlapAndDisjointClosuresPreserveFullEvidence(string scenario)
    {
        var fixture = Create(scenario);
        var cold = new Dictionary<string, string>();
        foreach (string name in new[] { "A", "B", "C" })
        {
            var run = fixture.Session();
            cold.Add(name, fixture.Capture(name, run.Context, run.Session));
            var flow = run.Session.Analyze(fixture.Root(name));
            Assert.Equal(name != "A" || scenario is "complete" or "constructed", flow.ProfileOutcome!.Complete);
            if (flow.ProfileOutcome.Complete)
                Assert.Equal(name == "C" ? "ArgumentException" : scenario == "constructed" ? "Fault" : "InvalidOperationException",
                    Assert.Single(flow.ThrownExceptions).Name);
            else Assert.Empty(flow.ThrownExceptions);
            output.WriteLine($"P2B_ROOT {scenario}/{name} " + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(cold[name]))));
            output.WriteLine(cold[name]);
        }

        foreach (string[] order in new[]
        {
            new[] { "A", "B" }, new[] { "B", "A" },
            new[] { "A", "B", "C" }, new[] { "C", "A", "B" },
            new[] { "A", "A", "A" }, new[] { "C", "B", "A", "B", "C", "A" }
        })
        {
            var run = fixture.Session();
            foreach (string name in order)
            {
                string actual = fixture.Capture(name, run.Context, run.Session);
                Assert.Equal(Encoding.UTF8.GetBytes(cold[name]), Encoding.UTF8.GetBytes(actual));
            }
        }
    }

    private static AdmissionCharacterization.Fixture Create(string scenario)
    {
        string a = scenario switch
        {
            "outside" => "Shared(); Bodyless();",
            "growth" => "Shared(); Grow<int>();",
            "constructed" => "Box<int>.Run<string>();",
            _ => "Shared();"
        };
        string b = scenario == "constructed" ? "Box<string>.Run<int>();" : "Shared();";
        return new([
            RootFile("A", a), RootFile("B", b), RootFile("C", "throw new System.ArgumentException();"),
            ("/P2B/Shared.cs", """
                public static partial class Entry
                {
                    static void Shared() { Leaf(); }
                    static void Leaf() { throw new System.InvalidOperationException(); }
                    static extern void Bodyless();
                    static void Grow<T>() { Grow<System.Collections.Generic.List<T>>(); }
                }
                public class Fault<T> : System.Exception {}
                public static class Box<T>
                {
                    public static void Run<U>() { throw new Fault<System.Collections.Generic.KeyValuePair<T,U>>(); }
                }
                """)
        ]);
    }

    private static (string Path, string Text) RootFile(string name, string body) => ($"/P2B/{name}.cs", $$"""
        public static partial class Entry
        {
            /// <summary>Root {{name}}.</summary>
            public static void {{name}}() { {{body}} }
        }
        """);
}
