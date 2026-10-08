using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.HistoricalWorker;

namespace XMLDocNormalizerTests.Worker
{
    /// <summary>Starts real fresh processes; never references the historical runtime in this test host.</summary>
    public sealed class HistoricalWorkerProcessTests
    {
        private const string Source = "public static class Fixture { public static void Root() { Thrower(); } static void Thrower() { throw null; } }";
        private const string HistoricalVersion = "5.0.0-2.25451.107";

        [Fact]
        public async Task Identity_ProvesSeparateProcessAndExactLoadedHistoricalUniverse()
        {
            Assembly common = typeof(Compilation).Assembly;
            Assembly csharp = typeof(CSharpCompilation).Assembly;
            Guid commonBefore = common.ManifestModule.ModuleVersionId;
            Guid csharpBefore = csharp.ManifestModule.ModuleVersionId;
            string infoBefore = common.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
            Assert.DoesNotContain(HistoricalVersion, infoBefore);

            ProcessResult execution = await Run(new(1, "identity"));
            Assert.NotEqual(Environment.ProcessId, execution.ProcessId);
            Assert.Equal(0, execution.ExitCode);
            Assert.Empty(execution.Stderr);
            using JsonDocument json = JsonDocument.Parse(execution.Stdout);
            JsonElement identity = json.RootElement.GetProperty("identity");
            Assert.Equal("XMLDocNormalizer.HistoricalWorker", identity.GetProperty("worker").GetProperty("name").GetString());
            Assert.Equal("XMLDocNormalizer.ExceptionFlow.Historical", identity.GetProperty("analyzer").GetProperty("name").GetString());
            Assert.False(identity.GetProperty("runtimeAwaitInformationAvailable").GetBoolean());
            JsonElement[] engines = identity.GetProperty("loadedRoslyn").EnumerateArray().ToArray();
            Assert.Equal(2, engines.Length);
            Assert.All(engines, engine =>
            {
                Assert.Equal("5.0.0.0", engine.GetProperty("assemblyVersion").GetString());
                Assert.StartsWith(HistoricalVersion + "+", engine.GetProperty("informationalVersion").GetString());
            });
            Assert.Equal("dc7738cc-6dca-4d34-9c44-29b53a7caa93", engines[0].GetProperty("mvid").GetString());
            Assert.Equal("0f9c1dcf-4eb1-47f8-81b2-733db5887be7", engines[1].GetProperty("mvid").GetString());
            Assert.Equal("660C3D626C4B8F4CF8C231FBEF0FB6B4DB4FFFCC89EF5B31AAECA1CF4D7F66A1", engines[0].GetProperty("sha256").GetString());
            Assert.Equal("B0EC1DDCA4C97DCF15845FF4BCEB5499C3989025197D5D65E04310E29C09217D", engines[1].GetProperty("sha256").GetString());
            Assert.Equal(commonBefore, common.ManifestModule.ModuleVersionId);
            Assert.Equal(csharpBefore, csharp.ManifestModule.ModuleVersionId);
            Assert.Equal(infoBefore, common.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion);
            Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), assembly =>
                assembly.GetName().Name == "XMLDocNormalizer.ExceptionFlow.Historical"
                || assembly.ManifestModule.ModuleVersionId.ToString("D") is "dc7738cc-6dca-4d34-9c44-29b53a7caa93" or "0f9c1dcf-4eb1-47f8-81b2-733db5887be7");

            string evidenceDirectory = Path.Combine(FindRoot(), "artifacts", "p5o2b3");
            Directory.CreateDirectory(evidenceDirectory);
            File.WriteAllText(Path.Combine(evidenceDirectory, "current-runtime-isolation.json"), JsonSerializer.Serialize(new
            {
                CallerProcessId = Environment.ProcessId,
                WorkerProcessId = execution.ProcessId,
                SeparateProcesses = true,
                HistoricalLoadedInCaller = false,
                CurrentCommon = new { Name = common.GetName().FullName, InformationalVersion = infoBefore, BeforeMvid = commonBefore,
                    AfterMvid = common.ManifestModule.ModuleVersionId, Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(common.Location))) },
                CurrentCSharp = new { Name = csharp.GetName().FullName, InformationalVersion = csharp.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion,
                    BeforeMvid = csharpBefore, AfterMvid = csharp.ManifestModule.ModuleVersionId, Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(csharp.Location))) },
                WorkerIdentity = identity
            }, WorkerProtocol.JsonOptions));
        }

        [Fact]
        public async Task RealTransitiveAnalysis_IsCanonicalAndDeterministicAcrossFreshWorkers()
        {
            WorkerRequest request = new(1, "analyze", new(Source, "Fixture", "Root"));
            ProcessResult first = await Run(request);
            ProcessResult second = await Run(request);
            Assert.NotEqual(first.ProcessId, second.ProcessId);
            Assert.Equal(0, first.ExitCode);
            Assert.Equal(0, second.ExitCode);
            Assert.Empty(first.Stderr);
            Assert.Empty(second.Stderr);
            Assert.Equal(first.Stdout, second.Stdout);
            using JsonDocument json = JsonDocument.Parse(first.Stdout);
            JsonElement result = json.RootElement.GetProperty("result");
            Assert.Empty(result.GetProperty("uncertainties").EnumerateArray());
            JsonElement entry = Assert.Single(result.GetProperty("entries").EnumerateArray());
            Assert.Equal("NullReferenceException", entry.GetProperty("exceptionType").GetProperty("metadataName").GetString());
            Assert.Equal("proven", entry.GetProperty("evidenceKind").GetString());
            JsonElement[] steps = entry.GetProperty("paths")[0].GetProperty("steps").EnumerateArray().ToArray();
            Assert.Equal(new[] { "methodCall", "explicitThrow" }, steps.Select(step => step.GetProperty("kind").GetString()));
            Assert.Equal("Fixture.Thrower()", steps[0].GetProperty("symbolName").GetString());
            Assert.All(steps, step => Assert.Equal("worker-input.cs", step.GetProperty("filePath").GetString()));
        }

        [Theory]
        [InlineData("{", "malformedInput")]
        [InlineData("", "malformedInput")]
        [InlineData("[]", "malformedInput")]
        [InlineData("{}{}", "malformedInput")]
        [InlineData("null", "invalidRequest")]
        [InlineData("{}", "invalidRequest")]
        [InlineData("{\"protocolVersion\":99,\"operation\":\"identity\"}", "unsupportedProtocolVersion")]
        [InlineData("{\"protocolVersion\":1,\"operation\":\"other\"}", "invalidRequest")]
        [InlineData("{\"protocolVersion\":1,\"operation\":\"analyze\"}", "invalidRequest")]
        [InlineData("{\"protocolVersion\":1,\"operation\":\"identity\",\"extra\":1}", "malformedInput")]
        public async Task InvalidTransport_IsStructuredFailClosed(string request, string code)
        {
            AssertFailure(await RunRaw(request), code);
        }

        [Fact]
        public async Task CompilationErrors_AreStructuredAndNeverSuccessful()
        {
            ProcessResult result = await Run(new(1, "analyze", new("public class Broken {", "Broken", "Root")));
            AssertFailure(result, "compilationFailure");
        }

        [Fact]
        public async Task AmbiguousOrMissingRoot_IsRejectedRatherThanReturningAnEmptySuccess()
        {
            AssertFailure(await Run(new(1, "analyze", new(Source, "Fixture", "Missing"))), "invalidRequest");
        }

        [Fact]
        public async Task HistoricalAwait_ActuallyPropagatesUnavailableInformationAsFailure()
        {
            const string source = "public static class Fixture { public static async System.Threading.Tasks.Task Root() { await System.Threading.Tasks.Task.CompletedTask; } }";
            ProcessResult result = await Run(new(1, "analyze", new(source, "Fixture", "Root")));
            AssertFailure(result, "analysisFailure");
            using JsonDocument json = JsonDocument.Parse(result.Stdout);
            Assert.Contains(json.RootElement.GetProperty("failure").GetProperty("details").EnumerateArray(),
                detail => detail.GetString() == "Await expression runtime-await information is unavailable.");
        }

        [Fact]
        public async Task RealCatchTransfer_SuppressesOnlyTheCaughtProvenException()
        {
            const string source = "public static class Fixture { public static void Root() { try { Thrower(); } catch (System.NullReferenceException) {} } static void Thrower() { throw null; } }";
            ProcessResult result = await Run(new(1, "analyze", new(source, "Fixture", "Root")));
            Assert.Equal(0, result.ExitCode);
            using JsonDocument json = JsonDocument.Parse(result.Stdout);
            Assert.Empty(json.RootElement.GetProperty("result").GetProperty("entries").EnumerateArray());
            Assert.Empty(json.RootElement.GetProperty("result").GetProperty("uncertainties").EnumerateArray());
        }

        [Fact]
        public async Task OversizedTransport_IsBoundedAndFailClosed()
            => AssertFailure(await RunRaw(new string(' ', WorkerProtocol.MaximumRequestCharacters + 1)), "invalidRequest");

        [Fact]
        public async Task InvalidUtf8_IsMalformedInputRatherThanAnUnhandledCrash()
            => AssertFailure(await RunBytes([0xff]), "malformedInput");

        [Fact]
        public void WorkerProject_HasOnlyTheHistoricalDependencyAndReusesB2Properties()
        {
            string directory = Path.Combine(FindRoot(), "src", "XMLDocNormalizer.HistoricalWorker");
            XDocument project = XDocument.Load(Path.Combine(directory, "XMLDocNormalizer.HistoricalWorker.csproj"));
            Assert.Equal("Exe", project.Descendants("OutputType").Single().Value);
            Assert.Equal("net8.0", project.Descendants("TargetFramework").Single().Value);
            Assert.Empty(project.Descendants("PackageReference"));
            Assert.Equal("../XMLDocNormalizer.ExceptionFlow.Historical/XMLDocNormalizer.ExceptionFlow.Historical.csproj",
                (string)project.Descendants("ProjectReference").Single().Attribute("Include")!);
            Assert.Equal("../XMLDocNormalizer.ExceptionFlow.Historical/Directory.Build.props",
                (string)XDocument.Load(Path.Combine(directory, "Directory.Build.props")).Descendants("Import").Single().Attribute("Project")!);
        }

        private static void AssertFailure(ProcessResult execution, string code)
        {
            Assert.Equal(1, execution.ExitCode);
            using JsonDocument json = JsonDocument.Parse(execution.Stdout);
            Assert.False(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal(code, json.RootElement.GetProperty("failure").GetProperty("code").GetString());
            Assert.False(json.RootElement.TryGetProperty("result", out _));
        }

        private static Task<ProcessResult> Run(WorkerRequest request)
            => RunRaw(JsonSerializer.Serialize(request, WorkerProtocol.JsonOptions));

        private static Task<ProcessResult> RunRaw(string request)
            => RunBytes(Encoding.UTF8.GetBytes(request));

        private static async Task<ProcessResult> RunBytes(byte[] request)
        {
            string configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
            string worker = Path.Combine(FindRoot(), "artifacts", "exception-flow-historical", "XMLDocNormalizer.HistoricalWorker",
                "bin", configuration, "net8.0", "XMLDocNormalizer.HistoricalWorker.dll");
            Assert.True(File.Exists(worker), "Build the permanent dual-version/worker gate before process tests: " + worker);
            ProcessStartInfo start = new("dotnet")
            {
                UseShellExecute = false, RedirectStandardInput = true,
                RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
            };
            start.ArgumentList.Add(worker);
            using Process process = Process.Start(start)!;
            Task<string> stdout = process.StandardOutput.ReadToEndAsync();
            Task<string> stderr = process.StandardError.ReadToEndAsync();
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
            try
            {
                try
                {
                    await process.StandardInput.BaseStream.WriteAsync(request.AsMemory(), timeout.Token);
                }
                catch (IOException)
                {
                    // The owned child may reject oversized input before the whole pipe is written.
                }
                finally
                {
                    process.StandardInput.Close();
                }

                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }

                await process.WaitForExitAsync();
                throw new TimeoutException("Owned Historical Worker exceeded its test deadline.");
            }

            return new ProcessResult(process.Id, process.ExitCode, (await stdout).Trim(), (await stderr).Trim());
        }

        private static string FindRoot()
        {
            for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "XMLDocNormalizer.sln")))
                {
                    return directory.FullName;
                }
            }

            throw new InvalidOperationException("Could not locate solution directory.");
        }

        private sealed record ProcessResult(int ProcessId, int ExitCode, string Stdout, string Stderr);
    }
}
