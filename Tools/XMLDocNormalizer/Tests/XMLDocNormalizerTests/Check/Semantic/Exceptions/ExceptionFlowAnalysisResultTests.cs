using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using XMLDocNormalizer.Models;
using XMLDocNormalizer.Models.DTO;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exceptions
{
    /// <summary>
    /// Tests storage, deduplication, merging, and removal of exception-flow paths.
    /// </summary>
    public sealed class ExceptionFlowAnalysisResultTests
    {
        /// <summary>
        /// Ensures that adding a path also registers its exception type.
        /// </summary>
        [Fact]
        public void AddExceptionPath_RegistersExceptionTypeAndPath()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(
                compilation,
                typeof(ArgumentException));

            ExceptionFlowPath path = CreatePath(
                line: 10,
                symbolName: "System.ArgumentException");

            ExceptionFlowAnalysisResult result = new();

            bool added = result.AddExceptionPath(
                exceptionType,
                path);

            Assert.True(added);
            Assert.Contains(
                exceptionType,
                result.ThrownExceptions);

            Assert.Same(
                path,
                Assert.Single(
                    result.GetExceptionPaths(exceptionType)));

            Assert.False(
                result.ArePathsTruncated(exceptionType));
        }

        /// <summary>
        /// Ensures that structurally identical paths are stored only once.
        /// </summary>
        [Fact]
        public void AddExceptionPath_DeduplicatesEquivalentPaths()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(
                compilation,
                typeof(InvalidOperationException));

            ExceptionFlowAnalysisResult result = new();

            bool firstAdded = result.AddExceptionPath(
                exceptionType,
                CreatePath(
                    line: 12,
                    symbolName:
                        "System.InvalidOperationException"));

            bool duplicateAdded = result.AddExceptionPath(
                exceptionType,
                CreatePath(
                    line: 12,
                    symbolName:
                        "System.InvalidOperationException"));

            Assert.True(firstAdded);
            Assert.False(duplicateAdded);

            Assert.Single(
                result.GetExceptionPaths(exceptionType));
        }

        /// <summary>
        /// Ensures that otherwise equal paths remain distinct when their
        /// source positions differ.
        /// </summary>
        [Fact]
        public void AddExceptionPath_PreservesDifferentCallSites()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(
                compilation,
                typeof(ArgumentNullException));

            ExceptionFlowAnalysisResult result = new();

            result.AddExceptionPath(
                exceptionType,
                CreatePath(
                    line: 20,
                    symbolName:
                        "System.ArgumentNullException"));

            result.AddExceptionPath(
                exceptionType,
                CreatePath(
                    line: 21,
                    symbolName:
                        "System.ArgumentNullException"));

            Assert.Equal(
                2,
                result.GetExceptionPaths(exceptionType).Count);
        }

        /// <summary>
        /// Ensures that merging with a prefix prepends the call-site step
        /// to every path.
        /// </summary>
        [Fact]
        public void MergeWithPrefix_PrependsStepToEveryPath()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(
                compilation,
                typeof(ArgumentOutOfRangeException));

            ExceptionFlowAnalysisResult source = new();

            source.AddExceptionPath(
                exceptionType,
                CreatePath(
                    line: 30,
                    symbolName:
                        "System.ArgumentOutOfRangeException"));

            source.UncertainTargets.Add(
                "External.Target()");

            ExceptionFlowPathStep prefix = new(
                ExceptionFlowPathStepKind.MethodCall,
                "Caller.Invoke()",
                "Caller.cs",
                8,
                13);

            ExceptionFlowAnalysisResult target = new();

            target.MergeWithPrefix(
                source,
                prefix);

            ExceptionFlowPath mergedPath =
                Assert.Single(
                    target.GetExceptionPaths(exceptionType));

            Assert.Equal(
                2,
                mergedPath.Steps.Count);

            Assert.Equal(
                prefix,
                mergedPath.Steps[0]);

            Assert.Contains(
                "External.Target()",
                target.UncertainTargets);
        }

        /// <summary>
        /// Ensures that the per-exception path limit is visible in the
        /// analysis result.
        /// </summary>
        [Fact]
        public void AddExceptionPath_MarksPathsAsTruncatedAtLimit()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(
                compilation,
                typeof(FileNotFoundException));

            ExceptionFlowAnalysisResult result = new();

            for (int index = 0;
                 index <
                 ExceptionFlowAnalysisResult
                     .MaximumPathsPerException + 1;
                 index++)
            {
                result.AddExceptionPath(
                    exceptionType,
                    CreatePath(
                        line: index + 1,
                        symbolName:
                            "System.IO.FileNotFoundException"));
            }

            Assert.Equal(
                ExceptionFlowAnalysisResult
                    .MaximumPathsPerException,
                result.GetExceptionPaths(exceptionType).Count);

            Assert.True(
                result.ArePathsTruncated(exceptionType));
        }

        /// <summary>
        /// Ensures that merging copies exception types, paths,
        /// truncation state, and uncertainty.
        /// </summary>
        [Fact]
        public void Merge_CopiesCompleteFlowState()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(
                compilation,
                typeof(ArgumentException));

            ExceptionFlowAnalysisResult source = new();

            source.AddExceptionPath(
                exceptionType,
                CreatePath(
                    line: 35,
                    symbolName:
                        "System.ArgumentException"));

            source.UncertainTargets.Add(
                "Unknown.Target()");

            ExceptionFlowAnalysisResult target = new();

            target.Merge(source);

            Assert.Contains(
                exceptionType,
                target.ThrownExceptions);

            Assert.Single(
                target.GetExceptionPaths(exceptionType));

            Assert.Contains(
                "Unknown.Target()",
                target.UncertainTargets);
        }

        /// <summary>
        /// Ensures that clearing thrown exceptions also clears all
        /// associated paths.
        /// </summary>
        [Fact]
        public void ClearThrownExceptions_RemovesTypesAndPaths()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(
                compilation,
                typeof(ArgumentException));

            ExceptionFlowAnalysisResult result = new();

            result.AddExceptionPath(
                exceptionType,
                CreatePath(
                    line: 36,
                    symbolName:
                        "System.ArgumentException"));

            result.ClearThrownExceptions();

            Assert.Empty(result.ThrownExceptions);

            Assert.Empty(
                result.GetExceptionPaths(exceptionType));
        }

        /// <summary>
        /// Ensures that removing an exception also removes all of its
        /// paths.
        /// </summary>
        [Fact]
        public void RemoveThrownExceptions_RemovesTypeAndPathsTogether()
        {
            CSharpCompilation compilation = CreateCompilation();

            INamedTypeSymbol argumentException =
                GetRequiredType(
                    compilation,
                    typeof(ArgumentException));

            INamedTypeSymbol invalidOperationException =
                GetRequiredType(
                    compilation,
                    typeof(InvalidOperationException));

            ExceptionFlowAnalysisResult result = new();

            result.AddExceptionPath(
                argumentException,
                CreatePath(
                    line: 40,
                    symbolName:
                        "System.ArgumentException"));

            result.AddExceptionPath(
                invalidOperationException,
                CreatePath(
                    line: 41,
                    symbolName:
                        "System.InvalidOperationException"));

            result.RemoveThrownExceptions(
                type =>
                    SymbolEqualityComparer.Default.Equals(
                        type,
                        argumentException));

            Assert.DoesNotContain(
                argumentException,
                result.ThrownExceptions);

            Assert.Empty(
                result.GetExceptionPaths(argumentException));

            Assert.Contains(
                invalidOperationException,
                result.ThrownExceptions);

            Assert.Single(
                result.GetExceptionPaths(
                    invalidOperationException));
        }

        /// <summary>
        /// Creates the Roslyn compilation used to resolve framework
        /// exception symbols.
        /// </summary>
        /// <returns>The created in-memory compilation.</returns>
        private static CSharpCompilation CreateCompilation()
        {
            return CSharpCompilation.Create(
                assemblyName:
                    "ExceptionFlowAnalysisResultTests",
                references:
                    MetadataReferences.Default,
                options:
                    new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary));
        }

        /// <summary>
        /// Resolves a required framework type from a compilation.
        /// </summary>
        /// <param name="compilation">
        /// The compilation used for type resolution.
        /// </param>
        /// <param name="runtimeType">
        /// The runtime type to resolve.
        /// </param>
        /// <returns>The resolved named type symbol.</returns>
        private static INamedTypeSymbol GetRequiredType(
            Compilation compilation,
            Type runtimeType)
        {
            INamedTypeSymbol? typeSymbol =
                compilation.GetTypeByMetadataName(
                    runtimeType.FullName!);

            return Assert.IsAssignableFrom<INamedTypeSymbol>(
                typeSymbol);
        }

        /// <summary>
        /// Creates one terminal exception-flow path at the specified
        /// source line.
        /// </summary>
        /// <param name="line">The one-based source line.</param>
        /// <param name="symbolName">
        /// The exception type display name.
        /// </param>
        /// <returns>The created exception-flow path.</returns>
        private static ExceptionFlowPath CreatePath(
            int line,
            string symbolName)
        {
            return new ExceptionFlowPath(
            [
                new ExceptionFlowPathStep(
                    ExceptionFlowPathStepKind.ExplicitThrow,
                    symbolName,
                    "Source.cs",
                    line,
                    9)
            ]);
        }
    }
}
