extern alias ExceptionFlowCore;

using System.Reflection;
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
    }
}
