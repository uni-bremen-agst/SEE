using Microsoft.CodeAnalysis;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests preservation of sequence-element facts across source-available
    /// helper calls.
    /// </summary>
    public sealed class ExceptionFlowSequenceSourceHelperTests
    {
        /// <summary>
        /// Ensures that materializing non-null grouping elements preserves
        /// their non-null guarantee.
        /// </summary>
        [Fact]
        public void GroupingMaterialization_PreservesNonNullElements()
        {
            const string source =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq;

                public static class EntryPoint
                {
                    public static void M(
                        IEnumerable<object?> candidates)
                    {
                        List<Item> items =
                            candidates
                                .OfType<Item>()
                                .ToList();

                        IEnumerable<IGrouping<int, Item>> groups =
                            items.GroupBy(
                                static _ => 0);

                        foreach (IGrouping<int, Item> group in groups)
                        {
                            List<Item> groupItems =
                                group.ToList();

                            ValidateAll(groupItems);
                        }
                    }

                    private static void ValidateAll(
                        IReadOnlyList<Item> items)
                    {
                        foreach (Item item in items)
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }
                }

                public sealed class Item
                {
                }
                """;

            AssertArgumentNullExceptionAbsentInBothModes(
                source);
        }

        /// <summary>
        /// Ensures that a source helper which only observes a materialized
        /// sequence does not invalidate its proven non-null element fact.
        /// </summary>
        [Fact]
        public void ReadOnlySourceHelper_PreservesNonNullElements()
        {
            const string source =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq;

                public static class EntryPoint
                {
                    public static void M(
                        IEnumerable<object?> candidates)
                    {
                        List<Item> items =
                            candidates
                                .OfType<Item>()
                                .ToList();

                        Observe(items);
                        ValidateAll(items);
                    }

                    private static void Observe(
                        IReadOnlyList<Item> items)
                    {
                        if (items == null ||
                            items.Count == 0)
                        {
                            return;
                        }

                        foreach (Item item in items)
                        {
                            _ = item;
                        }
                    }

                    private static void ValidateAll(
                        IReadOnlyList<Item> items)
                    {
                        foreach (Item item in items)
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }
                }

                public sealed class Item
                {
                }
                """;

            AssertArgumentNullExceptionAbsentInBothModes(
                source);
        }

        /// <summary>
        /// Ensures that a source helper which can insert a null element
        /// invalidates an earlier non-null element guarantee.
        /// </summary>
        [Fact]
        public void MutatingSourceHelper_InvalidatesNonNullElements()
        {
            const string source =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq;

                public static class EntryPoint
                {
                    public static void M(
                        IEnumerable<object?> candidates)
                    {
                        List<Item> items =
                            candidates
                                .OfType<Item>()
                                .ToList();

                        Mutate(items);
                        ValidateAll(items);
                    }

                    private static void Mutate(
                        List<Item> items)
                    {
                        items.Add(null);
                    }

                    private static void ValidateAll(
                        IReadOnlyList<Item> items)
                    {
                        foreach (Item item in items)
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }
                }

                public sealed class Item
                {
                }
                """;

            AssertArgumentNullExceptionPresentInBothModes(
                source);
        }

        /// <summary>
        /// Asserts that neither transitive analysis engine reports
        /// <see cref="ArgumentNullException"/>.
        /// </summary>
        /// <param name="source">
        /// The source to analyze.
        /// </param>
        private static void
            AssertArgumentNullExceptionAbsentInBothModes(
                string source)
        {
            ExceptionFlowAnalyzerTestRun projectRun =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeTransitively(
                        source,
                        "M");

            ExceptionFlowAnalyzerTestRun solutionRun =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeSolutionTransitively(
                        source,
                        "M");

            AssertArgumentNullExceptionAbsent(
                projectRun);

            AssertArgumentNullExceptionAbsent(
                solutionRun);
        }

        /// <summary>
        /// Asserts that both transitive analysis engines retain
        /// <see cref="ArgumentNullException"/>.
        /// </summary>
        /// <param name="source">
        /// The source to analyze.
        /// </param>
        private static void
            AssertArgumentNullExceptionPresentInBothModes(
                string source)
        {
            ExceptionFlowAnalyzerTestRun projectRun =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeTransitively(
                        source,
                        "M");

            ExceptionFlowAnalyzerTestRun solutionRun =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeSolutionTransitively(
                        source,
                        "M");

            AssertArgumentNullExceptionPresent(
                projectRun);

            AssertArgumentNullExceptionPresent(
                solutionRun);
        }

        /// <summary>
        /// Asserts that the specified result contains no
        /// <see cref="ArgumentNullException"/> path.
        /// </summary>
        /// <param name="run">
        /// The completed analyzer run.
        /// </param>
        private static void AssertArgumentNullExceptionAbsent(
            ExceptionFlowAnalyzerTestRun run)
        {
            INamedTypeSymbol argumentNullException =
                run.GetRequiredType(
                    "System.ArgumentNullException");

            Assert.Empty(
                run.Result.GetExceptionPaths(
                    argumentNullException));
        }

        /// <summary>
        /// Asserts that the specified result contains at least one
        /// <see cref="ArgumentNullException"/> path.
        /// </summary>
        /// <param name="run">
        /// The completed analyzer run.
        /// </param>
        private static void AssertArgumentNullExceptionPresent(
            ExceptionFlowAnalyzerTestRun run)
        {
            INamedTypeSymbol argumentNullException =
                run.GetRequiredType(
                    "System.ArgumentNullException");

            Assert.NotEmpty(
                run.Result.GetExceptionPaths(
                    argumentNullException));
        }
    }
}
