using System.Collections;
using System.Reflection;
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
        /// Ensures that a retained proven exception is merged with the requested prefix.
        /// </summary>
        [Fact]
        public void MergeWithPrefixExcluding_RetainsUncaughtProvenException()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(compilation, typeof(ArgumentException));
            ExceptionFlowPath sourcePath = CreatePath(31, "System.ArgumentException");
            ExceptionFlowAnalysisResult source = new();
            source.AddExceptionPath(exceptionType, sourcePath);

            ExceptionFlowPathStep prefix = CreatePrefix(9);
            ExceptionFlowAnalysisResult target = new();
            target.MergeWithPrefixExcluding(source, prefix, excludeException: _ => false);

            Assert.Contains(exceptionType, target.ThrownExceptions);
            ExceptionFlowPath mergedPath = Assert.Single(target.GetExceptionPaths(exceptionType));
            Assert.Equal(2, mergedPath.Steps.Count);
            Assert.Equal(prefix, mergedPath.Steps[0]);
            Assert.Equal(sourcePath.Steps[0], mergedPath.Steps[1]);
        }

        /// <summary>
        /// Ensures that a filtered proven exception and its paths are not merged.
        /// </summary>
        [Fact]
        public void MergeWithPrefixExcluding_ExcludesCaughtProvenException()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(compilation, typeof(ArgumentNullException));
            ExceptionFlowAnalysisResult source = new();
            source.AddExceptionPath(exceptionType, CreatePath(32, "System.ArgumentNullException"));

            ExceptionFlowAnalysisResult target = new();
            target.MergeWithPrefixExcluding(
                source,
                CreatePrefix(10),
                excludeException: type => SymbolEqualityComparer.Default.Equals(type, exceptionType));

            Assert.DoesNotContain(exceptionType, target.ThrownExceptions);
            Assert.Empty(target.GetExceptionPaths(exceptionType));
        }

        /// <summary>
        /// Ensures that retained external-documentation evidence receives the requested prefix.
        /// </summary>
        [Fact]
        public void MergeWithPrefixExcluding_RetainsUncaughtExternalEvidence()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(compilation, typeof(NotSupportedException));
            ExceptionFlowPath sourcePath = CreatePath(33, "System.NotSupportedException");
            ExceptionFlowAnalysisResult source = new();
            source.AddExternalDocumentationEvidencePath(exceptionType, sourcePath);

            ExceptionFlowPathStep prefix = CreatePrefix(11);
            ExceptionFlowAnalysisResult target = new();
            target.MergeWithPrefixExcluding(source, prefix, excludeException: _ => false);

            Assert.Contains(exceptionType, target.ExternalDocumentationEvidenceExceptions);
            ExceptionFlowPath mergedPath = Assert.Single(
                target.GetExternalDocumentationEvidencePaths(exceptionType));
            Assert.Equal(2, mergedPath.Steps.Count);
            Assert.Equal(prefix, mergedPath.Steps[0]);
            Assert.Equal(sourcePath.Steps[0], mergedPath.Steps[1]);
        }

        /// <summary>
        /// Ensures that filtered external-documentation evidence and its paths are not merged.
        /// </summary>
        [Fact]
        public void MergeWithPrefixExcluding_ExcludesCaughtExternalEvidence()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(compilation, typeof(FormatException));
            ExceptionFlowAnalysisResult source = new();
            source.AddExternalDocumentationEvidencePath(exceptionType, CreatePath(34, "System.FormatException"));

            ExceptionFlowAnalysisResult target = new();
            target.MergeWithPrefixExcluding(
                source,
                CreatePrefix(12),
                excludeException: type => SymbolEqualityComparer.Default.Equals(type, exceptionType));

            Assert.DoesNotContain(exceptionType, target.ExternalDocumentationEvidenceExceptions);
            Assert.Empty(target.GetExternalDocumentationEvidencePaths(exceptionType));
        }

        /// <summary>
        /// Ensures that retained exception types preserve source path truncation.
        /// </summary>
        [Fact]
        public void MergeWithPrefixExcluding_PropagatesRetainedTruncation()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(compilation, typeof(IOException));
            ExceptionFlowAnalysisResult source = new();
            AddPathsBeyondLimit(source, exceptionType, "System.IO.IOException");

            ExceptionFlowAnalysisResult target = new();
            target.MergeWithPrefixExcluding(source, CreatePrefix(13), excludeException: _ => false);

            Assert.Equal(
                ExceptionFlowAnalysisResult.MaximumPathsPerException,
                target.GetExceptionPaths(exceptionType).Count);
            Assert.True(target.ArePathsTruncated(exceptionType));
        }

        /// <summary>
        /// Ensures that uncertainty is unioned independently of exception filtering.
        /// </summary>
        [Fact]
        public void MergeWithPrefixExcluding_PreservesUncertainTargets()
        {
            ExceptionFlowAnalysisResult source = new();
            source.UncertainTargets.Add("Source.First()");
            source.UncertainTargets.Add("Source.Second()");

            ExceptionFlowAnalysisResult target = new();
            target.UncertainTargets.Add("Target.Existing()");
            target.MergeWithPrefixExcluding(source, CreatePrefix(14), excludeException: _ => true);

            Assert.Equal(3, target.UncertainTargets.Count);
            Assert.Contains("Target.Existing()", target.UncertainTargets);
            Assert.Contains("Source.First()", target.UncertainTargets);
            Assert.Contains("Source.Second()", target.UncertainTargets);
        }

        /// <summary>
        /// Ensures that filtering and prefixing do not mutate the source result or its paths.
        /// </summary>
        [Fact]
        public void MergeWithPrefixExcluding_DoesNotMutateSource()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(compilation, typeof(InvalidOperationException));
            ExceptionFlowPath provenPath = CreatePath(35, "System.InvalidOperationException");
            ExceptionFlowPath externalPath = CreatePath(36, "System.InvalidOperationException");
            ExceptionFlowAnalysisResult source = new();
            source.AddExceptionPath(exceptionType, provenPath);
            source.AddExternalDocumentationEvidencePath(exceptionType, externalPath);
            source.UncertainTargets.Add("Source.Unknown()");

            ExceptionFlowAnalysisResult target = new();
            target.MergeWithPrefixExcluding(source, CreatePrefix(15), excludeException: _ => true);

            Assert.Contains(exceptionType, source.ThrownExceptions);
            Assert.Same(provenPath, Assert.Single(source.GetExceptionPaths(exceptionType)));
            Assert.Contains(exceptionType, source.ExternalDocumentationEvidenceExceptions);
            Assert.Same(
                externalPath,
                Assert.Single(source.GetExternalDocumentationEvidencePaths(exceptionType)));
            Assert.Single(provenPath.Steps);
            Assert.Single(externalPath.Steps);
            Assert.Contains("Source.Unknown()", source.UncertainTargets);
        }

        /// <summary>
        /// Ensures that one merge applies different catch behavior per exception type.
        /// </summary>
        [Fact]
        public void MergeWithPrefixExcluding_AppliesCatchBehaviorPerType()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol caughtType = GetRequiredType(compilation, typeof(ArgumentException));
            INamedTypeSymbol firstUncaughtType = GetRequiredType(compilation, typeof(InvalidOperationException));
            INamedTypeSymbol secondUncaughtType = GetRequiredType(compilation, typeof(IOException));
            ExceptionFlowAnalysisResult source = new();
            source.AddExceptionPath(caughtType, CreatePath(37, "System.ArgumentException"));
            source.AddExceptionPath(firstUncaughtType, CreatePath(38, "System.InvalidOperationException"));
            source.AddExceptionPath(secondUncaughtType, CreatePath(39, "System.IO.IOException"));

            ExceptionFlowPathStep prefix = CreatePrefix(16);
            ExceptionFlowAnalysisResult target = new();
            target.MergeWithPrefixExcluding(
                source,
                prefix,
                excludeException: type => SymbolEqualityComparer.Default.Equals(type, caughtType));

            Assert.DoesNotContain(caughtType, target.ThrownExceptions);
            Assert.Empty(target.GetExceptionPaths(caughtType));

            INamedTypeSymbol[] uncaughtTypes = [firstUncaughtType, secondUncaughtType];
            foreach (INamedTypeSymbol uncaughtType in uncaughtTypes)
            {
                Assert.Contains(uncaughtType, target.ThrownExceptions);
                Assert.Equal(prefix, Assert.Single(target.GetExceptionPaths(uncaughtType)).Steps[0]);
            }
        }

        /// <summary>
        /// Ensures that directly merging retained paths preserves target path deduplication.
        /// </summary>
        [Fact]
        public void MergeWithPrefixExcluding_DeduplicatesPrefixedPaths()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(compilation, typeof(FileNotFoundException));
            ExceptionFlowPath sourcePath = CreatePath(40, "System.IO.FileNotFoundException");
            ExceptionFlowAnalysisResult source = new();
            source.AddExceptionPath(exceptionType, sourcePath);

            ExceptionFlowPathStep prefix = CreatePrefix(17);
            ExceptionFlowAnalysisResult target = new();
            target.AddExceptionPath(exceptionType, sourcePath.Prepend(prefix));
            target.MergeWithPrefixExcluding(source, prefix, excludeException: _ => false);

            Assert.Single(target.GetExceptionPaths(exceptionType));
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
        /// Ensures that an equivalent path offered after the collection is
        /// full does not mark the collection as truncated.
        /// </summary>
        [Fact]
        public void AddExceptionPath_DuplicateAtLimitDoesNotMarkPathsAsTruncated()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(
                compilation,
                typeof(FileNotFoundException));
            ExceptionFlowAnalysisResult result = new();

            for (int index = 0;
                 index < ExceptionFlowAnalysisResult.MaximumPathsPerException;
                 index++)
            {
                result.AddExceptionPath(
                    exceptionType,
                    CreatePath(
                        line: index + 1,
                        symbolName: "System.IO.FileNotFoundException"));
            }

            bool duplicateAdded = result.AddExceptionPath(
                exceptionType,
                CreatePath(
                    line: 1,
                    symbolName: "System.IO.FileNotFoundException"));

            Assert.False(duplicateAdded);
            Assert.Equal(
                ExceptionFlowAnalysisResult.MaximumPathsPerException,
                result.GetExceptionPaths(exceptionType).Count);
            Assert.False(result.ArePathsTruncated(exceptionType));
        }

        /// <summary>
        /// Ensures that prefix merging still checks for a duplicate while a
        /// full target collection has not yet been marked as truncated.
        /// </summary>
        [Fact]
        public void MergeWithPrefix_DuplicateAtLimitDoesNotMarkPathsAsTruncated()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(compilation, typeof(IOException));
            ExceptionFlowPathStep prefix = CreatePrefix(41);
            ExceptionFlowAnalysisResult target = new();
            AddPrefixedPathsToLimit(target, exceptionType, "System.IO.IOException", prefix);

            ExceptionFlowAnalysisResult source = new();
            source.AddExceptionPath(exceptionType, CreatePath(1, "System.IO.IOException"));
            target.MergeWithPrefix(source, prefix);

            Assert.Equal(
                ExceptionFlowAnalysisResult.MaximumPathsPerException,
                target.GetExceptionPaths(exceptionType).Count);
            Assert.False(target.ArePathsTruncated(exceptionType));
        }

        /// <summary>
        /// Ensures that prefix merging marks a full target collection as
        /// truncated only when the next path is actually new.
        /// </summary>
        [Fact]
        public void MergeWithPrefix_NewPathAtLimitMarksPathsAsTruncated()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(compilation, typeof(IOException));
            ExceptionFlowPathStep prefix = CreatePrefix(42);
            ExceptionFlowAnalysisResult target = new();
            AddPrefixedPathsToLimit(target, exceptionType, "System.IO.IOException", prefix);

            ExceptionFlowAnalysisResult source = new();
            source.AddExceptionPath(exceptionType, CreatePath(1000, "System.IO.IOException"));
            source.AddExceptionPath(exceptionType, CreatePath(1001, "System.IO.IOException"));
            target.MergeWithPrefix(source, prefix);

            Assert.Equal(
                ExceptionFlowAnalysisResult.MaximumPathsPerException,
                target.GetExceptionPaths(exceptionType).Count);
            Assert.True(target.ArePathsTruncated(exceptionType));
            Assert.Equal(2, source.GetExceptionPaths(exceptionType).Count);
        }

        /// <summary>
        /// Ensures that a saturated proven-path collection remains unchanged
        /// for both duplicate and new source paths while other state is merged.
        /// </summary>
        /// <param name="duplicate">
        /// Indicates whether the source path duplicates a retained path after
        /// prefixing.
        /// </param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MergeWithPrefix_SaturatedProvenCollectionPreservesState(bool duplicate)
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(compilation, typeof(IOException));
            ExceptionFlowPathStep prefix = CreatePrefix(43);
            ExceptionFlowAnalysisResult target = new();
            AddPrefixedPathsToLimit(target, exceptionType, "System.IO.IOException", prefix);
            target.AddExceptionPath(
                exceptionType,
                CreatePath(1000, "System.IO.IOException").Prepend(prefix));
            ExceptionFlowPath[] originalPaths = target.GetExceptionPaths(exceptionType).ToArray();

            ExceptionFlowAnalysisResult source = new();
            ExceptionFlowPath sourcePath = CreatePath(
                duplicate ? 1 : 2000,
                "System.IO.IOException");
            source.AddExceptionPath(exceptionType, sourcePath);
            source.UncertainTargets.Add("Source.Unknown()");

            target.MergeWithPrefix(source, prefix);

            Assert.Equal(originalPaths, target.GetExceptionPaths(exceptionType));
            Assert.True(target.ArePathsTruncated(exceptionType));
            Assert.Contains(exceptionType, target.ThrownExceptions);
            Assert.Contains("Source.Unknown()", target.UncertainTargets);
            Assert.Same(sourcePath, Assert.Single(source.GetExceptionPaths(exceptionType)));
            Assert.Single(sourcePath.Steps);
        }

        /// <summary>
        /// Ensures that a truncated collection below the path limit is not
        /// treated as saturated.
        /// </summary>
        [Fact]
        public void MergeWithPrefix_TruncatedCollectionBelowLimitStillAcceptsPath()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(compilation, typeof(IOException));
            ExceptionFlowAnalysisResult target = new();
            target.AddExceptionPath(exceptionType, CreatePath(1, "System.IO.IOException"));
            MarkPathCollectionTruncated(target, exceptionType, externalDocumentation: false);

            ExceptionFlowPathStep prefix = CreatePrefix(44);
            ExceptionFlowAnalysisResult source = new();
            source.AddExceptionPath(exceptionType, CreatePath(2, "System.IO.IOException"));
            target.MergeWithPrefix(source, prefix);

            IReadOnlyList<ExceptionFlowPath> paths = target.GetExceptionPaths(exceptionType);
            Assert.Equal(2, paths.Count);
            Assert.True(target.ArePathsTruncated(exceptionType));
            Assert.Contains(paths, path => path.Steps.Count == 2 && path.Steps[0] == prefix);
        }

        /// <summary>
        /// Ensures that a saturated external-evidence collection remains
        /// unchanged for both duplicate and new prefixed source paths.
        /// </summary>
        /// <param name="duplicate">
        /// Indicates whether the source path duplicates retained evidence
        /// after prefixing.
        /// </param>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MergeWithPrefix_SaturatedExternalEvidencePreservesState(bool duplicate)
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(compilation, typeof(NotSupportedException));
            ExceptionFlowPathStep prefix = CreatePrefix(45);
            ExceptionFlowAnalysisResult target = new();
            AddPrefixedExternalPathsToLimit(
                target,
                exceptionType,
                "System.NotSupportedException",
                prefix);
            target.AddExternalDocumentationEvidencePath(
                exceptionType,
                CreatePath(1000, "System.NotSupportedException").Prepend(prefix));
            ExceptionFlowPath[] originalPaths =
                target.GetExternalDocumentationEvidencePaths(exceptionType).ToArray();

            ExceptionFlowAnalysisResult source = new();
            ExceptionFlowPath sourcePath = CreatePath(
                duplicate ? 1 : 2000,
                "System.NotSupportedException");
            source.AddExternalDocumentationEvidencePath(exceptionType, sourcePath);

            target.MergeWithPrefix(source, prefix);

            Assert.Equal(
                originalPaths,
                target.GetExternalDocumentationEvidencePaths(exceptionType));
            Assert.True(GetPathCollectionTruncated(
                target,
                exceptionType,
                externalDocumentation: true));
            Assert.Contains(exceptionType, target.ExternalDocumentationEvidenceExceptions);
            Assert.Same(
                sourcePath,
                Assert.Single(source.GetExternalDocumentationEvidencePaths(exceptionType)));
            Assert.Single(sourcePath.Steps);
        }

        /// <summary>
        /// Ensures that external-evidence truncation is propagated even when
        /// prefix merging ends with a full target collection.
        /// </summary>
        [Fact]
        public void MergeWithPrefix_PropagatesExternalEvidenceTruncation()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(compilation, typeof(NotSupportedException));
            ExceptionFlowAnalysisResult source = new();

            for (int index = 0;
                 index < ExceptionFlowAnalysisResult.MaximumPathsPerException + 1;
                 index++)
            {
                source.AddExternalDocumentationEvidencePath(
                    exceptionType,
                    CreatePath(index + 1, "System.NotSupportedException"));
            }

            ExceptionFlowAnalysisResult target = new();
            target.MergeWithPrefix(source, CreatePrefix(46));

            Assert.Equal(
                ExceptionFlowAnalysisResult.MaximumPathsPerException,
                target.GetExternalDocumentationEvidencePaths(exceptionType).Count);
            Assert.True(GetPathCollectionTruncated(
                target,
                exceptionType,
                externalDocumentation: true));
        }

        /// <summary>
        /// Ensures that a new prefixed external-evidence path at the limit
        /// marks the target as truncated without consuming later source paths.
        /// </summary>
        [Fact]
        public void MergeWithPrefix_NewExternalPathAtLimitMarksTruncated()
        {
            CSharpCompilation compilation = CreateCompilation();
            INamedTypeSymbol exceptionType = GetRequiredType(compilation, typeof(NotSupportedException));
            ExceptionFlowPathStep prefix = CreatePrefix(47);
            ExceptionFlowAnalysisResult target = new();
            AddPrefixedExternalPathsToLimit(
                target,
                exceptionType,
                "System.NotSupportedException",
                prefix);

            ExceptionFlowAnalysisResult source = new();
            source.AddExternalDocumentationEvidencePath(
                exceptionType,
                CreatePath(1000, "System.NotSupportedException"));
            source.AddExternalDocumentationEvidencePath(
                exceptionType,
                CreatePath(1001, "System.NotSupportedException"));

            target.MergeWithPrefix(source, prefix);

            Assert.Equal(
                ExceptionFlowAnalysisResult.MaximumPathsPerException,
                target.GetExternalDocumentationEvidencePaths(exceptionType).Count);
            Assert.True(GetPathCollectionTruncated(
                target,
                exceptionType,
                externalDocumentation: true));
            Assert.Equal(
                2,
                source.GetExternalDocumentationEvidencePaths(exceptionType).Count);
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
        /// Adds exactly the configured number of distinct prefixed proven
        /// paths to an analysis result.
        /// </summary>
        /// <param name="result">The result receiving the paths.</param>
        /// <param name="exceptionType">The exception type for the paths.</param>
        /// <param name="symbolName">The terminal exception symbol name.</param>
        /// <param name="prefix">The prefix applied to every path.</param>
        private static void AddPrefixedPathsToLimit(
            ExceptionFlowAnalysisResult result,
            INamedTypeSymbol exceptionType,
            string symbolName,
            ExceptionFlowPathStep prefix)
        {
            for (int index = 0;
                 index < ExceptionFlowAnalysisResult.MaximumPathsPerException;
                 index++)
            {
                result.AddExceptionPath(
                    exceptionType,
                    CreatePath(index + 1, symbolName).Prepend(prefix));
            }
        }

        /// <summary>
        /// Adds exactly the configured number of distinct prefixed external
        /// evidence paths to an analysis result.
        /// </summary>
        /// <param name="result">The result receiving the paths.</param>
        /// <param name="exceptionType">The exception type for the paths.</param>
        /// <param name="symbolName">The terminal exception symbol name.</param>
        /// <param name="prefix">The prefix applied to every path.</param>
        private static void AddPrefixedExternalPathsToLimit(
            ExceptionFlowAnalysisResult result,
            INamedTypeSymbol exceptionType,
            string symbolName,
            ExceptionFlowPathStep prefix)
        {
            for (int index = 0;
                 index < ExceptionFlowAnalysisResult.MaximumPathsPerException;
                 index++)
            {
                result.AddExternalDocumentationEvidencePath(
                    exceptionType,
                    CreatePath(index + 1, symbolName).Prepend(prefix));
            }
        }

        /// <summary>
        /// Marks one private path collection as truncated without filling it,
        /// allowing the negative saturation guard to be tested without a
        /// production test hook.
        /// </summary>
        /// <param name="result">The result containing the collection.</param>
        /// <param name="exceptionType">The exception type identifying it.</param>
        /// <param name="externalDocumentation">
        /// Indicates whether the external-evidence collection is requested.
        /// </param>
        private static void MarkPathCollectionTruncated(
            ExceptionFlowAnalysisResult result,
            INamedTypeSymbol exceptionType,
            bool externalDocumentation)
        {
            object collection = GetPathCollection(
                result,
                exceptionType,
                externalDocumentation);
            MethodInfo? markTruncated = collection.GetType().GetMethod(
                "MarkTruncated",
                BindingFlags.Instance | BindingFlags.Public);

            Assert.NotNull(markTruncated);
            markTruncated!.Invoke(collection, parameters: null);
        }

        /// <summary>
        /// Gets the truncation state of one private path collection.
        /// </summary>
        /// <param name="result">The result containing the collection.</param>
        /// <param name="exceptionType">The exception type identifying it.</param>
        /// <param name="externalDocumentation">
        /// Indicates whether the external-evidence collection is requested.
        /// </param>
        /// <returns>The collection's truncation state.</returns>
        private static bool GetPathCollectionTruncated(
            ExceptionFlowAnalysisResult result,
            INamedTypeSymbol exceptionType,
            bool externalDocumentation)
        {
            object collection = GetPathCollection(
                result,
                exceptionType,
                externalDocumentation);
            PropertyInfo? pathsTruncated = collection.GetType().GetProperty(
                "PathsTruncated",
                BindingFlags.Instance | BindingFlags.Public);

            Assert.NotNull(pathsTruncated);
            return Assert.IsType<bool>(pathsTruncated!.GetValue(collection));
        }

        /// <summary>
        /// Gets one private path collection for focused state verification.
        /// </summary>
        /// <param name="result">The result containing the collection.</param>
        /// <param name="exceptionType">The exception type identifying it.</param>
        /// <param name="externalDocumentation">
        /// Indicates whether the external-evidence collection is requested.
        /// </param>
        /// <returns>The requested private collection.</returns>
        private static object GetPathCollection(
            ExceptionFlowAnalysisResult result,
            INamedTypeSymbol exceptionType,
            bool externalDocumentation)
        {
            string fieldName = externalDocumentation
                ? "externalDocumentationEvidencePaths"
                : "exceptionPaths";
            FieldInfo? collectionField = typeof(ExceptionFlowAnalysisResult).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(collectionField);
            IDictionary collections = Assert.IsAssignableFrom<IDictionary>(
                collectionField!.GetValue(result));
            object? collection = collections[exceptionType];
            Assert.NotNull(collection);
            return collection!;
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

        /// <summary>
        /// Creates one call-site prefix at the specified source line.
        /// </summary>
        /// <param name="line">The one-based source line.</param>
        /// <returns>The created call-site prefix.</returns>
        private static ExceptionFlowPathStep CreatePrefix(int line)
        {
            return new ExceptionFlowPathStep(
                ExceptionFlowPathStepKind.MethodCall,
                "Caller.Invoke()",
                "Caller.cs",
                line,
                13);
        }
    }
}
