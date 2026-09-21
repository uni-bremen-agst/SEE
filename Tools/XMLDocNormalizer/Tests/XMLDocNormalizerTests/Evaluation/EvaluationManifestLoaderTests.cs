using System.Text.Json;
using XMLDocNormalizer.Evaluation;

namespace XMLDocNormalizerTests.Evaluation
{
    /// <summary>
    /// Tests the offline manifest and workspace trust boundary.
    /// </summary>
    public sealed class EvaluationManifestLoaderTests
    {
        /// <summary>
        /// Ensures candidates and artifacts are sorted by stable identifiers.
        /// </summary>
        [Fact]
        public void Load_ValidManifest_ReturnsStableOrdering()
        {
            string path = WriteManifest(CreateManifest(candidateIds: ["z", "a"]));

            try
            {
                EvaluationManifest manifest = EvaluationManifestLoader.Load(path);

                Assert.Equal(["a", "z"], manifest.Candidates.Select(static candidate => candidate.Id));
                Assert.Equal(["a", "z"], manifest.Artifacts.Select(static artifact => artifact.Id));
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Ensures floating package versions are rejected.
        /// </summary>
        [Theory]
        [InlineData("latest")]
        [InlineData("1.*")]
        public void Load_FloatingVersion_RejectsManifest(string version)
        {
            EvaluationManifest manifest = CreateManifest();
            manifest.Candidates[0].Version = version;
            AssertInvalid(manifest);
        }

        /// <summary>
        /// Ensures absolute developer paths cannot enter versioned candidate data.
        /// </summary>
        [Fact]
        public void Load_AbsoluteCandidatePath_RejectsManifest()
        {
            EvaluationManifest manifest = CreateManifest();
            manifest.Candidates[0].AssemblyPath = Path.GetFullPath("candidate.dll");
            AssertInvalid(manifest);
        }

        /// <summary>
        /// Ensures parent traversal cannot enter a candidate path.
        /// </summary>
        [Fact]
        public void Load_TraversalCandidatePath_RejectsManifest()
        {
            EvaluationManifest manifest = CreateManifest();
            manifest.Candidates[0].AssemblyPath = "../candidate.dll";
            AssertInvalid(manifest);
        }

        /// <summary>
        /// Ensures preparation artifacts require HTTPS.
        /// </summary>
        [Fact]
        public void Load_HttpArtifact_RejectsManifest()
        {
            EvaluationManifest manifest = CreateManifest();
            manifest.Artifacts[0].Url = "http://example.test/package.nupkg";
            AssertInvalid(manifest);
        }

        /// <summary>
        /// Ensures malformed artifact hashes fail closed.
        /// </summary>
        [Fact]
        public void Load_InvalidArtifactHash_RejectsManifest()
        {
            EvaluationManifest manifest = CreateManifest();
            manifest.Artifacts[0].Sha256 = "ABC";
            AssertInvalid(manifest);
        }

        /// <summary>
        /// Ensures a normal workspace-relative path resolves inside the root.
        /// </summary>
        [Fact]
        public void ResolveWorkspacePath_ContainedPath_ReturnsAbsolutePath()
        {
            string workspace = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            string resolved = EvaluationManifestLoader.ResolveWorkspacePath(
                Path.GetFullPath(workspace),
                "packages/example.dll");

            Assert.StartsWith(Path.GetFullPath(workspace), resolved, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Ensures workspace traversal is rejected even when called independently.
        /// </summary>
        [Fact]
        public void ResolveWorkspacePath_Traversal_Throws()
        {
            string workspace = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

            Assert.Throws<InvalidOperationException>(() =>
                EvaluationManifestLoader.ResolveWorkspacePath(
                    Path.GetFullPath(workspace),
                    "../outside.dll"));
        }

        private static EvaluationManifest CreateManifest(string[]? candidateIds = null)
        {
            string[] ids = candidateIds ?? ["candidate"];
            return new EvaluationManifest
            {
                SchemaVersion = 1,
                Artifacts = ids.Select(id => new EvaluationArtifact
                {
                    Id = id,
                    Url = "https://example.test/" + id + ".nupkg",
                    Sha256 = new string('A', 64),
                    ExtractTo = "packages/" + id
                }).ToList(),
                Candidates = ids.Select(id => new EvaluationCandidate
                {
                    Id = id,
                    Package = id,
                    Version = "1.2.3",
                    TargetFramework = "net8.0",
                    AssemblyPath = "packages/" + id + "/lib/net8.0/" + id + ".dll",
                    ExpectedOutcome = EvaluationExpectedOutcome.Success,
                    SelectionReason = "Test"
                }).ToList()
            };
        }

        private static void AssertInvalid(EvaluationManifest manifest)
        {
            string path = WriteManifest(manifest);

            try
            {
                Assert.Throws<InvalidOperationException>(() => EvaluationManifestLoader.Load(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static string WriteManifest(EvaluationManifest manifest)
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
            File.WriteAllText(path, JsonSerializer.Serialize(manifest));
            return path;
        }
    }
}
