using System.Xml.Linq;

namespace XMLDocNormalizerTests.Check.Semantic.Exceptions
{
    /// <summary>
    /// Cheap project-contract checks. The separate build/CI gate, not each unit
    /// test, restores and compiles the historical assembly.
    /// </summary>
    public sealed class HistoricalAnalyzerBuildProjectTests
    {
        /// <summary>Preserves the exact original boundary plus the real capability.</summary>
        [Fact]
        public void SharedManifest_MatchesProvenBoundaryAndCurrentPhysicalSources()
        {
            string root = FindRoot();
            string manifestPath = Path.Combine(root, "build", "ExceptionFlow.HistoricalSources.props");
            string probePath = Path.Combine(root, "Evaluation", "P5O2B1CompileProbe", "HistoricalSources.props");
            string[] shared = ReadSources(manifestPath);
            string[] original = ReadSources(probePath);

            Assert.Equal(120, original.Length);
            Assert.Equal(121, shared.Length);
            Assert.Equal(121, shared.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.All(original, source => Assert.Contains(source, shared));
            Assert.Single(shared.Except(original, StringComparer.OrdinalIgnoreCase),
                source => source.EndsWith("ExceptionFlowRuntimeAwaitCapability.cs", StringComparison.Ordinal));
            string mainRoot = Path.Combine(root, "src", "XMLDocNormalizer") + Path.DirectorySeparatorChar;
            Assert.All(shared, source =>
            {
                Assert.StartsWith(mainRoot, source);
                Assert.True(File.Exists(source), source);
            });
        }

        /// <summary>Rejects floating versions, umbrella packages and runtime references.</summary>
        [Fact]
        public void HistoricalProject_PinsOnlyTheFiveProvenDependencies()
        {
            XDocument project = ReadHistoricalProject();
            Dictionary<string, string> packages = project.Descendants("PackageReference")
                .ToDictionary(item => (string)item.Attribute("Include")!,
                    item => (string)item.Attribute("Version")!);

            Assert.Equal(5, packages.Count);
            Assert.Equal("[5.0.0-2.25451.107]", packages["Microsoft.CodeAnalysis.Common"]);
            Assert.Equal("[5.0.0-2.25451.107]", packages["Microsoft.CodeAnalysis.CSharp"]);
            Assert.Equal("[3.11.0]", packages["Microsoft.CodeAnalysis.Analyzers"]);
            Assert.Equal("[9.0.0]", packages["System.Collections.Immutable"]);
            Assert.Equal("[9.0.0]", packages["System.Reflection.Metadata"]);
            Assert.Empty(project.Descendants("ProjectReference"));
            Assert.Equal("net8.0", project.Descendants("TargetFramework").Single().Value);
            Assert.Equal("false", project.Descendants("EnableDefaultCompileItems").Single().Value);
        }

        /// <summary>No production source substitution or Evaluation dependency is allowed.</summary>
        [Fact]
        public void HistoricalProject_UsesPermanentManifestAndOnlyBuildHostLocally()
        {
            XDocument project = ReadHistoricalProject();
            Assert.Equal("../../build/ExceptionFlow.HistoricalSources.props",
                (string)project.Descendants("Import").Single().Attribute("Project")!);
            Assert.Equal("BuildOnlySemanticEnvironment.cs",
                (string)project.Descendants("Compile").Single().Attribute("Include")!);
            Assert.DoesNotContain("Evaluation", project.ToString());
            Assert.Empty(project.Descendants("Compile").Where(item => item.Attribute("Remove") != null));
        }

        /// <summary>Early SDK properties keep generated code and package assets private.</summary>
        [Fact]
        public void HistoricalOutputs_AreConfiguredBeforeSdkImports()
        {
            XDocument props = XDocument.Load(Path.Combine(HistoricalDirectory(), "Directory.Build.props"));
            Assert.Contains("artifacts/exception-flow-historical/",
                props.Descendants("HistoricalArtifactsRoot").Single().Value);
            Assert.Contains("$(HistoricalArtifactsRoot)", props.Descendants("BaseIntermediateOutputPath").Single().Value);
            Assert.Equal("$(BaseIntermediateOutputPath)", props.Descendants("MSBuildProjectExtensionsPath").Single().Value);
            Assert.Contains("$(HistoricalArtifactsRoot)", props.Descendants("BaseOutputPath").Single().Value);
            Assert.Contains("$(HistoricalArtifactsRoot)", props.Descendants("RestorePackagesPath").Single().Value);
            Assert.Equal("$(MSBuildThisFileDirectory)NuGet.Config", props.Descendants("RestoreConfigFile").Single().Value);
        }

        /// <summary>Normal solution/tests must not obtain a historical runtime reference.</summary>
        [Fact]
        public void HistoricalOwner_RemainsOutsideNormalSolutionAndProjectGraph()
        {
            string root = FindRoot();
            Assert.DoesNotContain("ExceptionFlow.Historical", File.ReadAllText(Path.Combine(root, "XMLDocNormalizer.sln")));
            string[] normalProjects =
            [
                "src/XMLDocNormalizer/XMLDocNormalizer.csproj",
                "src/XMLDocNormalizer.ExceptionFlow.Core/XMLDocNormalizer.ExceptionFlow.Core.csproj",
                "Tests/XMLDocNormalizerTests/XMLDocNormalizerTests.csproj",
                "Evaluation/XMLDocNormalizer.Evaluation/XMLDocNormalizer.Evaluation.csproj"
            ];
            Assert.All(normalProjects, path => Assert.DoesNotContain("ExceptionFlow.Historical",
                File.ReadAllText(Path.Combine(root, path))));
            Assert.Equal("XMLDocNormalizer.ExceptionFlow.Historical",
                ReadHistoricalProject().Descendants("AssemblyName").Single().Value);
        }

        /// <summary>The build target declares and enforces its non-executable host contract.</summary>
        [Fact]
        public void BuildHost_IsExplicitlyNonExecutableAndContainsNoAnalyzerPolicy()
        {
            string host = File.ReadAllText(Path.Combine(HistoricalDirectory(), "BuildOnlySemanticEnvironment.cs"));
            Assert.Equal(6, host.Split("throw new NotSupportedException", StringSplitOptions.None).Length - 1);
            Assert.Contains("internal ExceptionFlowSemanticEnvironment()", host);
            Assert.DoesNotContain("ProjectClosure", host);
            Assert.DoesNotContain("Assembly.Load", host);
            Assert.Contains("BuildOnly; requires an isolated runtime host", ReadHistoricalProject().ToString());
        }

        /// <summary>Historical compiler packages have a dedicated source mapping.</summary>
        [Fact]
        public void HistoricalRestore_MapsCompilerPackagesToTheRecordedFeed()
        {
            XDocument config = XDocument.Load(Path.Combine(HistoricalDirectory(), "NuGet.Config"));
            XElement source = config.Descendants("packageSources").Elements("add")
                .Single(item => (string?)item.Attribute("key") == "historical-roslyn");
            Assert.Contains("d1622942-d16f-48e5-bc83-96f4539e7601", (string)source.Attribute("value")!);
            XElement mapping = config.Descendants("packageSource")
                .Single(item => (string?)item.Attribute("key") == "historical-roslyn");
            Assert.Equal(new[] { "Microsoft.CodeAnalysis.Common", "Microsoft.CodeAnalysis.CSharp" },
                mapping.Elements("package").Select(item => (string)item.Attribute("pattern")!).ToArray());
            Assert.DoesNotContain(config.Descendants("package"), item => (string?)item.Attribute("pattern") == "*");
        }

        /// <summary>The local project exception does not require altering root ignore rules.</summary>
        [Fact]
        public void PermanentProject_HasANarrowLocalIgnoreException()
        {
            Assert.Equal("!XMLDocNormalizer.ExceptionFlow.Historical.csproj",
                File.ReadAllText(Path.Combine(HistoricalDirectory(), ".gitignore")).Trim());
        }

        /// <summary>Resolves explicit shared source items without running MSBuild.</summary>
        private static string[] ReadSources(string manifestPath)
        {
            string directory = Path.GetDirectoryName(manifestPath)! + Path.DirectorySeparatorChar;
            return XDocument.Load(manifestPath).Descendants("Compile")
                .Select(item => Path.GetFullPath(((string)item.Attribute("Include")!)
                    .Replace("$(MSBuildThisFileDirectory)", directory, StringComparison.Ordinal)))
                .ToArray();
        }

        /// <summary>Reads the permanent historical project.</summary>
        private static XDocument ReadHistoricalProject()
            => XDocument.Load(Path.Combine(HistoricalDirectory(), "XMLDocNormalizer.ExceptionFlow.Historical.csproj"));

        /// <summary>Locates the permanent historical owner.</summary>
        private static string HistoricalDirectory()
            => Path.Combine(FindRoot(), "src", "XMLDocNormalizer.ExceptionFlow.Historical");

        /// <summary>Finds the solution ancestor from source or built test execution.</summary>
        private static string FindRoot()
        {
            foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                for (DirectoryInfo? directory = new(start); directory != null; directory = directory.Parent)
                {
                    if (File.Exists(Path.Combine(directory.FullName, "XMLDocNormalizer.sln")))
                    {
                        return directory.FullName;
                    }
                }
            }

            throw new InvalidOperationException("Could not locate XMLDocNormalizer.sln.");
        }
    }
}
