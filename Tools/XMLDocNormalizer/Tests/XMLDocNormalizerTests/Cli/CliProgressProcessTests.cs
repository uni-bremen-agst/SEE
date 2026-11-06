using System.Diagnostics;
using System.Text.Json;
using XMLDocNormalizer.Execution;

namespace XMLDocNormalizerTests.Cli;

[Collection("Console-dependent tests")]
public sealed class CliProgressProcessTests
{
    [Theory]
    [InlineData("direct", 0)]
    [InlineData("project-transitive", 3)]
    [InlineData("solution-transitive", 3)]
    [InlineData("project-transitive-declared-exceptions", 3)]
    public async Task ActualCliKeepsExitCodesFindingsCoverageAndStreamBoundaries(string mode, int contexts)
    {
        string root = Path.Combine(Path.GetTempPath(), "cli1-process-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string project = Path.Combine(root, "Fixture.csproj"), config = Path.Combine(root, "NuGet.Config");
            File.WriteAllText(project, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Library</OutputType></PropertyGroup></Project>");
            File.WriteAllText(config, "<configuration><packageSources><clear /></packageSources></configuration>");
            File.WriteAllText(Path.Combine(root, "Source.cs"), """
                class Fault : System.Exception {}
                /// <summary>Container.</summary>
                class Entry {
                    /// <summary>Run.</summary>
                    /// <exception cref="Fault">Unused.</exception>
                    public void Run() { A(); A(); }
                    static void A() { B(); }
                    static void B() {}
                }
                """);
            var restore = await Run(root, "restore", project, "--configfile", config, "-p:NuGetAudit=false");
            Assert.True(restore.Code == 0, restore.Output + restore.Error);
            string tool = typeof(ToolRunner).Assembly.Location;
            var off = await Run(root, tool, "--check", "--progress", "off", "--exception-analysis-mode", mode, "--format", "json", "--output", Path.Combine(root, "off.json"), project);
            var on = await Run(root, tool, "--check", "--progress", "on", "--verbose", "--exception-analysis-mode", mode, "--format", "json", "--output", Path.Combine(root, "on.json"), project);
            Assert.Equal(ToolExitCodes.Findings, off.Code);
            Assert.Equal(off.Code, on.Code);
            Assert.Equal("", off.Error);
            Assert.DoesNotContain("Completed |", on.Output); Assert.DoesNotContain("Current |", on.Output);
            Assert.Contains("Pending | [ ] Load inputs", on.Error);
            Assert.Contains("Pending | [ ] Prepare compilations", on.Error);
            Assert.Contains("Completed | [ok] Documentation; 1/1 files", on.Error);
            Assert.Contains("roots completed 1, started 1", on.Error);
            if (mode == "direct")
            {
                Assert.DoesNotContain("contexts/workitems", on.Error);
                Assert.DoesNotContain("contexts discovered 0", on.Error);
            }
            else
            {
                Assert.Contains("contexts discovered " + contexts, on.Error);
                Assert.Contains("context workitems processed " + contexts, on.Error);
            }
            foreach (string phase in new[] { "Load inputs", "Prepare compilations", "Documentation", "Exception flow", "Finalize and report" })
                Assert.Contains("Completed | [ok] " + phase, on.Error);
            Assert.True(on.Error.IndexOf("Completed | [ok] Load inputs", StringComparison.Ordinal) < on.Error.IndexOf("Current | [>] Prepare compilations", StringComparison.Ordinal));
            Assert.True(on.Error.IndexOf("Completed | [ok] Prepare compilations", StringComparison.Ordinal) < on.Error.IndexOf("Current | Documentation", StringComparison.Ordinal));
            Assert.True(on.Error.IndexOf("Completed | [ok] Documentation", StringComparison.Ordinal) < on.Error.IndexOf("Completed | [ok] Exception flow", StringComparison.Ordinal));
            Assert.True(on.Error.IndexOf("Completed | [ok] Exception flow", StringComparison.Ordinal) < on.Error.IndexOf("Current | [>] Finalize and report", StringComparison.Ordinal));
            Assert.True(on.Error.IndexOf("Current | [>] Finalize and report", StringComparison.Ordinal) < on.Error.IndexOf("Completed | [ok] Finalize and report", StringComparison.Ordinal));
            Assert.DoesNotContain('%', on.Error);
            Assert.DoesNotContain('\u001b', on.Error);
            Assert.True(on.Error.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length <= 20);
            using var baseline = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "off.json")));
            using var observed = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "on.json")));
            Assert.Equal(baseline.RootElement.GetProperty("Findings").GetRawText(), observed.RootElement.GetProperty("Findings").GetRawText());
            Assert.Equal(baseline.RootElement.GetProperty("Analysis").GetRawText(), observed.RootElement.GetProperty("Analysis").GetRawText());
        }
        finally
        {
            string target = Path.GetFullPath(root);
            if (!target.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)
                || !Path.GetFileName(target).StartsWith("cli1-process-", StringComparison.Ordinal)) throw new InvalidOperationException("Unexpected cleanup path");
            Directory.Delete(target, true);
        }
    }

    private static async Task<(int Code, string Output, string Error)> Run(string directory, params string[] arguments)
    {
        ProcessStartInfo start = new("dotnet") { WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = start };
        Assert.True(process.Start());
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, await stdout, await stderr);
    }
}
