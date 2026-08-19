using Microsoft.CodeAnalysis;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Verifies that source-method return analysis uses value facts mapped
    /// into the invoked method's call context and preserves those facts for
    /// stable local values.
    /// </summary>
    public sealed class DOC611_SourceReturnCallContextTests
    {
        /// <summary>
        /// Ensures that a method with multiple return paths can preserve a
        /// known non-null argument when its result is consumed directly.
        /// </summary>
        [Fact]
        public void MultipleReturnSourceMethod_KnownNonNullArgument_DirectUse_ProvesReturnNonNull()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        bool useReplacement)
                    {
                        Validate(
                            Normalize(
                                new Item(),
                                useReplacement));
                    }

                    private static Item? Normalize(
                        Item? item,
                        bool useReplacement)
                    {
                        if (useReplacement)
                        {
                            return new Item();
                        }

                        return item;
                    }

                    private static void Validate(
                        Item? item)
                    {
                        ArgumentNullException.ThrowIfNull(
                            item);
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionAbsentInBothTransitiveModes(
                source);
        }

        /// <summary>
        /// Ensures that a method with multiple return paths can preserve a
        /// known non-null argument when its result is first stored in a local.
        /// </summary>
        [Fact]
        public void MultipleReturnSourceMethod_KnownNonNullArgument_LocalUse_ProvesReturnNonNull()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        bool useReplacement)
                    {
                        Item? normalized =
                            Normalize(
                                new Item(),
                                useReplacement);

                        Validate(
                            normalized);
                    }

                    private static Item? Normalize(
                        Item? item,
                        bool useReplacement)
                    {
                        if (useReplacement)
                        {
                            return new Item();
                        }

                        return item;
                    }

                    private static void Validate(
                        Item? item)
                    {
                        ArgumentNullException.ThrowIfNull(
                            item);
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionAbsentInBothTransitiveModes(
                source);
        }

        /// <summary>
        /// Ensures that an unknown argument remains potentially null when one
        /// normal return path returns that argument.
        /// </summary>
        [Fact]
        public void MultipleReturnSourceMethod_UnknownArgument_RemainsPotentiallyNull()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        Item? item,
                        bool useReplacement)
                    {
                        Item? normalized =
                            Normalize(
                                item,
                                useReplacement);

                        Validate(
                            normalized);
                    }

                    private static Item? Normalize(
                        Item? item,
                        bool useReplacement)
                    {
                        if (useReplacement)
                        {
                            return new Item();
                        }

                        return item;
                    }

                    private static void Validate(
                        Item? item)
                    {
                        ArgumentNullException.ThrowIfNull(
                            item);
                    }

                    public sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothTransitiveModes(
                source);
        }

        /// <summary>
        /// Ensures that a known non-null argument cannot hide another normal
        /// return path that explicitly produces null.
        /// </summary>
        [Fact]
        public void MultipleReturnSourceMethod_ExplicitNullReturn_RemainsPotentiallyNull()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        bool returnNull)
                    {
                        Item? normalized =
                            Normalize(
                                new Item(),
                                returnNull);

                        Validate(
                            normalized);
                    }

                    private static Item? Normalize(
                        Item? item,
                        bool returnNull)
                    {
                        if (returnNull)
                        {
                            return null;
                        }

                        return item;
                    }

                    private static void Validate(
                        Item? item)
                    {
                        ArgumentNullException.ThrowIfNull(
                            item);
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothTransitiveModes(
                source);
        }

        /// <summary>
        /// Ensures that a non-null initializer fact is not retained after the
        /// local is explicitly reassigned to null.
        /// </summary>
        [Fact]
        public void KnownNonNullReturnStoredInLocal_ReassignedToNull_RemainsPotentiallyNull()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        bool useReplacement)
                    {
                        Item? normalized =
                            Normalize(
                                new Item(),
                                useReplacement);

                        normalized = null;

                        Validate(
                            normalized);
                    }

                    private static Item? Normalize(
                        Item? item,
                        bool useReplacement)
                    {
                        if (useReplacement)
                        {
                            return new Item();
                        }

                        return item;
                    }

                    private static void Validate(
                        Item? item)
                    {
                        ArgumentNullException.ThrowIfNull(
                            item);
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothTransitiveModes(
                source);
        }

        /// <summary>
        /// Asserts that neither transitive mode reports an
        /// <see cref="ArgumentNullException"/> path for the supplied source.
        /// </summary>
        /// <param name="source">
        /// The complete source to analyze.
        /// </param>
        private static void
            AssertArgumentNullExceptionAbsentInBothTransitiveModes(
                string source)
        {
            ExceptionFlowAnalyzerTestRun projectRun =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(
                    source,
                    "M");

            ExceptionFlowAnalyzerTestRun solutionRun =
                ExceptionFlowAnalyzerTestHelper.AnalyzeSolutionTransitively(
                    source,
                    "M");

            AssertArgumentNullExceptionAbsent(
                projectRun);

            AssertArgumentNullExceptionAbsent(
                solutionRun);
        }

        /// <summary>
        /// Asserts that both transitive modes retain an
        /// <see cref="ArgumentNullException"/> path for the supplied source.
        /// </summary>
        /// <param name="source">
        /// The complete source to analyze.
        /// </param>
        private static void
            AssertArgumentNullExceptionPresentInBothTransitiveModes(
                string source)
        {
            ExceptionFlowAnalyzerTestRun projectRun =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(
                    source,
                    "M");

            ExceptionFlowAnalyzerTestRun solutionRun =
                ExceptionFlowAnalyzerTestHelper.AnalyzeSolutionTransitively(
                    source,
                    "M");

            AssertArgumentNullExceptionPresent(
                projectRun);

            AssertArgumentNullExceptionPresent(
                solutionRun);
        }

        /// <summary>
        /// Asserts that one analyzer run contains no
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
        /// Asserts that one analyzer run contains an
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

        /// <summary>
        /// Ensures that a reassignment occurring after the guarded use does not
        /// invalidate the non-null initializer fact at the earlier use site.
        /// </summary>
        [Fact]
        public void KnownNonNullReturnStoredInLocal_ReassignedAfterUse_RemainsNonNullAtUse()
        {
            const string source =
                """
        #nullable enable
        using System;

        public static class EntryPoint
        {
            public static void M(
                bool useReplacement)
            {
                Item? normalized =
                    Normalize(
                        new Item(),
                        useReplacement);

                Validate(
                    normalized);

                normalized = null;
            }

            private static Item? Normalize(
                Item? item,
                bool useReplacement)
            {
                if (useReplacement)
                {
                    return new Item();
                }

                return item;
            }

            private static void Validate(
                Item? item)
            {
                ArgumentNullException.ThrowIfNull(
                    item);
            }

            private sealed class Item
            {
            }
        }
        """;

            AssertArgumentNullExceptionAbsentInBothTransitiveModes(
                source);
        }

        /// <summary>
        /// Ensures that a possible reassignment in a preceding conditional branch
        /// invalidates the non-null initializer fact.
        /// </summary>
        [Fact]
        public void KnownNonNullReturnStoredInLocal_ConditionallyReassignedBeforeUse_RemainsPotentiallyNull()
        {
            const string source =
                """
        #nullable enable
        using System;

        public static class EntryPoint
        {
            public static void M(
                bool useReplacement,
                bool clear)
            {
                Item? normalized =
                    Normalize(
                        new Item(),
                        useReplacement);

                if (clear)
                {
                    normalized = null;
                }

                Validate(
                    normalized);
            }

            private static Item? Normalize(
                Item? item,
                bool useReplacement)
            {
                if (useReplacement)
                {
                    return new Item();
                }

                return item;
            }

            private static void Validate(
                Item? item)
            {
                ArgumentNullException.ThrowIfNull(
                    item);
            }

            private sealed class Item
            {
            }
        }
        """;

            AssertArgumentNullExceptionPresentInBothTransitiveModes(
                source);
        }

        /// <summary>
        /// Ensures that entering a nested block without an intervening write keeps a
        /// proven non-null initializer fact.
        /// </summary>
        [Fact]
        public void KnownNonNullReturnStoredInLocal_NestedUseWithoutWrite_RemainsNonNull()
        {
            const string source =
                """
        #nullable enable
        using System;

        public static class EntryPoint
        {
            public static void M(
                bool useReplacement,
                bool execute)
            {
                Item? normalized =
                    Normalize(
                        new Item(),
                        useReplacement);

                if (execute)
                {
                    Validate(
                        normalized);
                }
            }

            private static Item? Normalize(
                Item? item,
                bool useReplacement)
            {
                if (useReplacement)
                {
                    return new Item();
                }

                return item;
            }

            private static void Validate(
                Item? item)
            {
                ArgumentNullException.ThrowIfNull(
                    item);
            }

            private sealed class Item
            {
            }
        }
        """;

            AssertArgumentNullExceptionAbsentInBothTransitiveModes(
                source);
        }
    }
}
