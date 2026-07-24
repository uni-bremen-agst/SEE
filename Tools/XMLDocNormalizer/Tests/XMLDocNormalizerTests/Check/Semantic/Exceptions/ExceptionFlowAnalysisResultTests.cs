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
        /// Ensures that merging copies exception types, paths, and
        /// uncertainty.
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
        /// Ensures that merging propagates a truncated path state.
        /// </summary>
        [Fact]
        public void Merge_PropagatesTruncationState()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(
                compilation,
                typeof(ArgumentException));

            ExceptionFlowAnalysisResult source = new();

            AddPathsBeyondLimit(
                source,
                exceptionType,
                "System.ArgumentException");

            ExceptionFlowAnalysisResult target = new();

            target.Merge(source);

            Assert.Equal(
                ExceptionFlowAnalysisResult
                    .MaximumPathsPerException,
                target.GetExceptionPaths(exceptionType).Count);

            Assert.True(
                target.ArePathsTruncated(exceptionType));
        }

        /// <summary>
        /// Ensures that merging does not mutate the source result.
        /// </summary>
        [Fact]
        public void Merge_DoesNotMutateSource()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(
                compilation,
                typeof(InvalidOperationException));

            ExceptionFlowPath sourcePath = CreatePath(
                line: 50,
                symbolName:
                    "System.InvalidOperationException");

            ExceptionFlowAnalysisResult source = new();

            source.AddExceptionPath(
                exceptionType,
                sourcePath);

            source.UncertainTargets.Add(
                "Unknown.SourceTarget()");

            ExceptionFlowAnalysisResult target = new();

            target.Merge(source);

            target.AddExceptionPath(
                exceptionType,
                CreatePath(
                    line: 51,
                    symbolName:
                        "System.InvalidOperationException"));

            target.UncertainTargets.Add(
                "Target.Only()");

            Assert.Same(
                sourcePath,
                Assert.Single(
                    source.GetExceptionPaths(exceptionType)));

            Assert.False(
                source.ArePathsTruncated(exceptionType));

            Assert.Contains(
                "Unknown.SourceTarget()",
                source.UncertainTargets);

            Assert.DoesNotContain(
                "Target.Only()",
                source.UncertainTargets);
        }

        /// <summary>
        /// Ensures that merging does not duplicate a path already present
        /// in the target result.
        /// </summary>
        [Fact]
        public void Merge_DeduplicatesExistingTargetPaths()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(
                compilation,
                typeof(FileNotFoundException));

            ExceptionFlowAnalysisResult source = new();
            ExceptionFlowAnalysisResult target = new();

            source.AddExceptionPath(
                exceptionType,
                CreatePath(
                    line: 55,
                    symbolName:
                        "System.IO.FileNotFoundException"));

            target.AddExceptionPath(
                exceptionType,
                CreatePath(
                    line: 55,
                    symbolName:
                        "System.IO.FileNotFoundException"));

            target.Merge(source);

            Assert.Single(
                target.GetExceptionPaths(exceptionType));
        }

        /// <summary>
        /// Ensures that merging a result with itself does not duplicate
        /// paths or otherwise change the result.
        /// </summary>
        [Fact]
        public void MergeWithItself_RemainsStable()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(
                compilation,
                typeof(ArgumentException));

            ExceptionFlowPath path = CreatePath(
                line: 60,
                symbolName:
                    "System.ArgumentException");

            ExceptionFlowAnalysisResult result = new();

            result.AddExceptionPath(
                exceptionType,
                path);

            result.UncertainTargets.Add(
                "Unknown.Target()");

            result.Merge(result);

            Assert.Contains(
                exceptionType,
                result.ThrownExceptions);

            Assert.Same(
                path,
                Assert.Single(
                    result.GetExceptionPaths(exceptionType)));

            Assert.False(
                result.ArePathsTruncated(exceptionType));

            Assert.Contains(
                "Unknown.Target()",
                result.UncertainTargets);
        }

        /// <summary>
        /// Ensures that prefix merging propagates a truncated path state.
        /// </summary>
        [Fact]
        public void MergeWithPrefix_PropagatesTruncationState()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(
                compilation,
                typeof(ArgumentException));

            ExceptionFlowAnalysisResult source = new();

            AddPathsBeyondLimit(
                source,
                exceptionType,
                "System.ArgumentException");

            ExceptionFlowPathStep prefix = new(
                ExceptionFlowPathStepKind.MethodCall,
                "Caller.Invoke()",
                "Caller.cs",
                12,
                9);

            ExceptionFlowAnalysisResult target = new();

            target.MergeWithPrefix(
                source,
                prefix);

            Assert.Equal(
                ExceptionFlowAnalysisResult
                    .MaximumPathsPerException,
                target.GetExceptionPaths(exceptionType).Count);

            Assert.True(
                target.ArePathsTruncated(exceptionType));

            foreach (ExceptionFlowPath path
                     in target.GetExceptionPaths(exceptionType))
            {
                Assert.Equal(
                    prefix,
                    path.Steps[0]);
            }
        }

        /// <summary>
        /// Ensures that prefix merging does not mutate source paths.
        /// </summary>
        [Fact]
        public void MergeWithPrefix_DoesNotMutateSource()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(
                compilation,
                typeof(InvalidOperationException));

            ExceptionFlowPath sourcePath = CreatePath(
                line: 65,
                symbolName:
                    "System.InvalidOperationException");

            ExceptionFlowAnalysisResult source = new();

            source.AddExceptionPath(
                exceptionType,
                sourcePath);

            ExceptionFlowPathStep prefix = new(
                ExceptionFlowPathStepKind.MethodCall,
                "Caller.Invoke()",
                "Caller.cs",
                14,
                11);

            ExceptionFlowAnalysisResult target = new();

            target.MergeWithPrefix(
                source,
                prefix);

            ExceptionFlowPath mergedPath =
                Assert.Single(
                    target.GetExceptionPaths(exceptionType));

            Assert.Same(
                sourcePath,
                Assert.Single(
                    source.GetExceptionPaths(exceptionType)));

            Assert.Single(sourcePath.Steps);

            Assert.Equal(
                2,
                mergedPath.Steps.Count);

            Assert.Equal(
                prefix,
                mergedPath.Steps[0]);

            Assert.Equal(
                sourcePath.Steps[0],
                mergedPath.Steps[1]);
        }

        /// <summary>
        /// Ensures that different prefix call sites remain distinct even
        /// when they lead to the same source path.
        /// </summary>
        [Fact]
        public void MergeWithPrefix_PreservesDifferentCallSites()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(
                compilation,
                typeof(ArgumentNullException));

            ExceptionFlowAnalysisResult source = new();

            source.AddExceptionPath(
                exceptionType,
                CreatePath(
                    line: 70,
                    symbolName:
                        "System.ArgumentNullException"));

            ExceptionFlowPathStep firstPrefix = new(
                ExceptionFlowPathStepKind.MethodCall,
                "Caller.Invoke()",
                "Caller.cs",
                20,
                9);

            ExceptionFlowPathStep secondPrefix = new(
                ExceptionFlowPathStepKind.MethodCall,
                "Caller.Invoke()",
                "Caller.cs",
                21,
                9);

            ExceptionFlowAnalysisResult target = new();

            target.MergeWithPrefix(
                source,
                firstPrefix);

            target.MergeWithPrefix(
                source,
                secondPrefix);

            IReadOnlyList<ExceptionFlowPath> mergedPaths =
                target.GetExceptionPaths(exceptionType);

            Assert.Equal(
                2,
                mergedPaths.Count);

            Assert.Contains(
                mergedPaths,
                path =>
                    path.Steps[0] == firstPrefix);

            Assert.Contains(
                mergedPaths,
                path =>
                    path.Steps[0] == secondPrefix);
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
        /// Adds more distinct paths than the configured per-exception
        /// limit permits.
        /// </summary>
        /// <param name="result">
        /// The result receiving the paths.
        /// </param>
        /// <param name="exceptionType">
        /// The exception type associated with the paths.
        /// </param>
        /// <param name="symbolName">
        /// The terminal exception symbol name.
        /// </param>
        private static void AddPathsBeyondLimit(
            ExceptionFlowAnalysisResult result,
            INamedTypeSymbol exceptionType,
            string symbolName)
        {
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
                        symbolName: symbolName));
            }
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
                new ExceptionFlowPathStep(
                    ExceptionFlowPathStepKind.ExplicitThrow,
                    symbolName,
                    "Source.cs",
                    line,
                    9));
        }
    }
}
