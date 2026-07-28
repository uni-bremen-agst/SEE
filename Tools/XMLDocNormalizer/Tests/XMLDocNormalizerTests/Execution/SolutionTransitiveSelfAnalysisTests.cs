using System.Diagnostics;
using XMLDocNormalizer.Execution;

namespace XMLDocNormalizerTests.Execution
{
    /// <summary>
    /// Verifies that solution-transitive analysis can process the
    /// XMLDocNormalizer production project without terminating unexpectedly.
    /// </summary>
    [Collection("Console-dependent tests")]
    public sealed class SolutionTransitiveSelfAnalysisTests
    {
        /// <summary>
        /// Ensures that solution-transitive self-analysis completes without
        /// an unhandled exception.
        /// </summary>
        [Fact]
        public async Task ProductionProject_CompletesWithoutUnhandledException()
        {
            string solutionPath = FindSolutionPath();
            string toolAssemblyPath =
                typeof(ToolExitCodes).Assembly.Location;

            string outputPath = Path.Combine(
                Path.GetTempPath(),
                $"xmldoc-self-analysis-{Guid.NewGuid():N}.json");

            ProcessStartInfo startInfo = new()
            {
                FileName = "dotnet",
                WorkingDirectory =
                    Path.GetDirectoryName(solutionPath)
                    ?? throw new InvalidOperationException(
                        "The solution directory could not be determined."),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add(toolAssemblyPath);
            startInfo.ArgumentList.Add("--check");
            startInfo.ArgumentList.Add("--project");
            startInfo.ArgumentList.Add("XMLDocNormalizer");
            startInfo.ArgumentList.Add(
                "--exception-analysis-mode");
            startInfo.ArgumentList.Add(
                "solution-transitive");
            startInfo.ArgumentList.Add("--format");
            startInfo.ArgumentList.Add("json");
            startInfo.ArgumentList.Add("--output");
            startInfo.ArgumentList.Add(outputPath);
            startInfo.ArgumentList.Add(solutionPath);

            try
            {
                using Process process =
                    Process.Start(startInfo)
                    ?? throw new InvalidOperationException(
                        "The XMLDocNormalizer process could not be started.");

                Task<string> standardOutputTask =
                    process.StandardOutput.ReadToEndAsync();

                Task<string> standardErrorTask =
                    process.StandardError.ReadToEndAsync();

                using CancellationTokenSource timeout = new(
                    TimeSpan.FromMinutes(3));

                try
                {
                    await process.WaitForExitAsync(
                        timeout.Token);
                }
                catch (OperationCanceledException)
                {
                    TryKill(process);

                    string timedOutOutput =
                        await standardOutputTask;

                    string timedOutError =
                        await standardErrorTask;

                    Assert.Fail(
                        "Solution-transitive self-analysis did not " +
                        "terminate within the configured timeout." +
                        Environment.NewLine +
                        "Standard output:" +
                        Environment.NewLine +
                        timedOutOutput +
                        Environment.NewLine +
                        "Standard error:" +
                        Environment.NewLine +
                        timedOutError);
                }

                string standardOutput =
                    await standardOutputTask;

                string standardError =
                    await standardErrorTask;

                Assert.True(
                    process.ExitCode ==
                    ToolExitCodes.Findings,
                    "Solution-transitive self-analysis terminated with " +
                    $"unexpected exit code {process.ExitCode}." +
                    Environment.NewLine +
                    "Standard output:" +
                    Environment.NewLine +
                    standardOutput +
                    Environment.NewLine +
                    "Standard error:" +
                    Environment.NewLine +
                    standardError);

                Assert.True(
                    File.Exists(outputPath),
                    "Solution-transitive self-analysis did not create " +
                    "the expected JSON report." +
                    Environment.NewLine +
                    "Standard output:" +
                    Environment.NewLine +
                    standardOutput +
                    Environment.NewLine +
                    "Standard error:" +
                    Environment.NewLine +
                    standardError);

                Assert.DoesNotContain(
                    "Unhandled exception.",
                    standardError,
                    StringComparison.OrdinalIgnoreCase);

                Assert.DoesNotContain(
                    "Unhandled exception.",
                    standardOutput,
                    StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
            }
        }

        /// <summary>
        /// Locates the repository solution from the test execution
        /// directory.
        /// </summary>
        /// <returns>The absolute solution path.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the solution cannot be located.
        /// </exception>
        private static string FindSolutionPath()
        {
            string[] startingDirectories =
            [
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            ];

            foreach (string startingDirectory
                     in startingDirectories)
            {
                DirectoryInfo? directory =
                    new(startingDirectory);

                while (directory != null)
                {
                    string candidate = Path.Combine(
                        directory.FullName,
                        "XMLDocNormalizer.sln");

                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }

                    directory = directory.Parent;
                }
            }

            throw new InvalidOperationException(
                "Could not locate XMLDocNormalizer.sln from the " +
                "current test execution directories.");
        }

        /// <summary>
        /// Attempts to terminate a timed-out child process.
        /// </summary>
        /// <param name="process">
        /// The child process to terminate.
        /// </param>
        private static void TryKill(Process process)
        {
            if (process.HasExited)
            {
                return;
            }

            try
            {
                process.Kill(
                    entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // The process terminated between the state check and kill.
            }
        }
    }
}
