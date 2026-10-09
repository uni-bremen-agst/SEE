extern alias ExceptionFlowCore;

using System.Reflection;
using System.Xml.Linq;
using CoreCanonicalAssemblyIdentity =
    ExceptionFlowCore::XMLDocNormalizer.Checks.Infrastructure.Exception.Flow.Canonical.CanonicalAssemblyIdentity;

namespace XMLDocNormalizerTests.Check.Semantic.Exceptions
{
    /// <summary>
    /// Guards the Roslyn-neutral dependency boundary of the shared
    /// exception-flow core.
    /// </summary>
    public sealed class ExceptionFlowCoreDependencyTests
    {
        /// <summary>The Core is a real source-linked neutral validation assembly, not a binary fixture.</summary>
        [Fact]
        public void CoreProject_CompilesExistingSharedSourcesWithoutRuntimeDependencies()
        {
            string coreDirectory = FindCoreDirectory();
            XDocument project = XDocument.Load(Path.Combine(coreDirectory, "XMLDocNormalizer.ExceptionFlow.Core.csproj"));
            Assert.Equal("false", project.Descendants("EnableDefaultCompileItems").Single().Value);
            Assert.Empty(project.Descendants("ProjectReference"));
            Assert.Empty(project.Descendants("PackageReference"));
            XElement[] sources = project.Descendants("Compile").ToArray();
            Assert.NotEmpty(sources);
            Assert.All(sources, source =>
            {
                string include = ((string)source.Attribute("Include")!).Replace('\\', '/');
                Assert.StartsWith("../XMLDocNormalizer/", include, StringComparison.Ordinal);
                Assert.NotNull(source.Attribute("Link"));
                string path = Path.GetFullPath(Path.Combine(coreDirectory, include));
                Assert.NotEmpty(Directory.GetFiles(Path.GetDirectoryName(path)!, Path.GetFileName(path)));
            });
        }

        /// <summary>Keep the genuine project trackable while excluding all generated Core output.</summary>
        [Fact]
        public void CoreProject_HasNarrowIgnoreExceptionAndGeneratedOutputRules()
            => Assert.Equal(new[] { "!XMLDocNormalizer.ExceptionFlow.Core.csproj", "bin/", "obj/" },
                File.ReadAllLines(Path.Combine(FindCoreDirectory(), ".gitignore")));

        /// <summary>
        /// Ensures that the shared exception-flow core has no compiler,
        /// workspace, build, or main-executable assembly dependency.
        /// </summary>
        [Fact]
        public void CoreAssembly_HasOnlyFrameworkAssemblyReferences()
        {
            Assembly coreAssembly = typeof(CoreCanonicalAssemblyIdentity).Assembly;
            AssemblyName[] references = coreAssembly.GetReferencedAssemblies();

            Assert.Equal(
                "XMLDocNormalizer.ExceptionFlow.Core",
                coreAssembly.GetName().Name);
            Assert.All(
                references,
                reference => Assert.StartsWith(
                    "System.",
                    reference.Name,
                    StringComparison.Ordinal));
            Assert.DoesNotContain(
                references,
                reference =>
                    string.Equals(
                        reference.Name,
                        "XMLDocNormalizer",
                        StringComparison.Ordinal)
                    || reference.Name?.StartsWith(
                        "Microsoft.CodeAnalysis",
                        StringComparison.Ordinal) == true
                    || reference.Name?.StartsWith(
                        "Microsoft.Build",
                        StringComparison.Ordinal) == true);
        }

        private static string FindCoreDirectory()
        {
            foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            {
                for (DirectoryInfo? directory = new(start); directory != null; directory = directory.Parent)
                {
                    string core = Path.Combine(directory.FullName, "src", "XMLDocNormalizer.ExceptionFlow.Core");
                    if (File.Exists(Path.Combine(core, "XMLDocNormalizer.ExceptionFlow.Core.csproj")))
                    {
                        return core;
                    }
                }
            }

            throw new InvalidOperationException("Could not locate the real ExceptionFlow.Core project.");
        }
    }
}
