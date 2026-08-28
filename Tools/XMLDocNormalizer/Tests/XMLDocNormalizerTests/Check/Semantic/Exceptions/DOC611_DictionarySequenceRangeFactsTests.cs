using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests non-null sequence facts propagated through dictionary storage,
    /// successful <c>TryGetValue</c>, grouping, and list range additions.
    /// </summary>
    public sealed class DOC611_DictionarySequenceRangeFactsTests
    {
        /// <summary>
        /// Ensures a safely populated list stored in a private nested
        /// dictionary remains safe after retrieval and <c>AddRange</c>.
        /// </summary>
        [Fact]
        public void SafeStoredSequenceAndAddRange_DoesNotProduceFinding()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class TestClass
                {
                    private sealed class Prepared
                    {
                        public Dictionary<string, List<object?>> ValuesByFile { get; } =
                            new(StringComparer.Ordinal);
                    }

                    /// <summary>
                    /// Combines previously prepared values.
                    /// </summary>
                    public static void M()
                    {
                        Prepared prepared = new();

                        List<object?> baseline = new();
                        baseline.Add(new object());

                        prepared.ValuesByFile["a"] = baseline;

                        List<object?> combined = new();

                        if (prepared.ValuesByFile.TryGetValue("a", out List<object?>? stored))
                        {
                            combined.AddRange(stored);
                        }

                        List<object?> additional = new();
                        additional.Add(new object());
                        combined.AddRange(additional);

                        Consume(combined);
                    }

                    private static void Consume(IReadOnlyList<object?> values)
                    {
                        foreach (object? value in values)
                        {
                            Validate(value);
                        }
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            AssertNoFindings(source);
        }

        /// <summary>
        /// Ensures the dictionary sequence invariant survives preparation in
        /// one method and consumption in another method.
        /// </summary>
        [Fact]
        public void StoredSequenceAcrossMethodBoundary_DoesNotProduceFinding()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class TestClass
                {
                    private sealed class Prepared
                    {
                        public Dictionary<string, List<object?>> ValuesByFile { get; } =
                            new(StringComparer.Ordinal);
                    }

                    /// <summary>
                    /// Prepares and consumes stored values.
                    /// </summary>
                    public static void M()
                    {
                        Prepared prepared = Prepare();
                        Execute(prepared);
                    }

                    private static Prepared Prepare()
                    {
                        Prepared prepared = new();

                        List<object?> baseline = new();
                        baseline.Add(new object());

                        ValidateAll(baseline);
                        prepared.ValuesByFile["a"] = baseline;

                        return prepared;
                    }

                    private static void Execute(Prepared prepared)
                    {
                        List<object?> combined = new();

                        if (prepared.ValuesByFile.TryGetValue("a", out List<object?>? baseline))
                        {
                            combined.AddRange(baseline);
                        }

                        Consume(combined);
                    }

                    private static void ValidateAll(IReadOnlyList<object?> values)
                    {
                        foreach (object? value in values)
                        {
                            _ = value.ToString();
                        }
                    }

                    private static void Consume(IReadOnlyList<object?> values)
                    {
                        foreach (object? value in values)
                        {
                            Validate(value);
                        }
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            AssertNoFindings(source);
        }

        /// <summary>
        /// Ensures the dictionary sequence invariant survives the same grouped
        /// append pattern used while preparing comparison findings.
        /// </summary>
        [Fact]
        public void GroupedAppendAcrossMethodBoundary_DoesNotProduceFinding()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;
                using System.Linq;

                public static class TestClass
                {
                    private sealed class Prepared
                    {
                        public Dictionary<string, List<object?>> ValuesByFile { get; } =
                            new(StringComparer.Ordinal);
                    }

                    /// <summary>
                    /// Prepares, augments, and consumes stored values.
                    /// </summary>
                    public static void M()
                    {
                        Prepared prepared = Prepare();
                        Execute(prepared);
                    }

                    private static Prepared Prepare()
                    {
                        Prepared prepared = new();

                        List<object?> baseline = new();
                        baseline.Add(new object());

                        ValidateAll(baseline);
                        prepared.ValuesByFile["a"] = baseline;

                        List<object?> additional = new();
                        additional.Add(new object());

                        foreach (IGrouping<string, object?> group in additional.GroupBy(_ => "a"))
                        {
                            if (!prepared.ValuesByFile.TryGetValue(
                                    group.Key,
                                    out List<object?>? existing))
                            {
                                existing = new List<object?>();
                                prepared.ValuesByFile[group.Key] = existing;
                            }

                            existing.AddRange(group);
                        }

                        return prepared;
                    }

                    private static void Execute(Prepared prepared)
                    {
                        List<object?> combined = new();

                        if (prepared.ValuesByFile.TryGetValue("a", out List<object?>? baseline))
                        {
                            combined.AddRange(baseline);
                        }

                        List<object?> exceptionValues = new();
                        exceptionValues.Add(new object());

                        ValidateAll(exceptionValues);
                        combined.AddRange(exceptionValues);

                        Consume(combined);
                    }

                    private static void ValidateAll(IReadOnlyList<object?> values)
                    {
                        foreach (object? value in values)
                        {
                            _ = value.ToString();
                        }
                    }

                    private static void Consume(IReadOnlyList<object?> values)
                    {
                        foreach (object? value in values)
                        {
                            Validate(value);
                        }
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            AssertNoFindings(source);
        }

        /// <summary>
        /// Ensures a possible null insertion before dictionary storage prevents
        /// propagation of the sequence-element fact.
        /// </summary>
        [Fact]
        public void PossibleNullBeforeDictionaryStorage_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class TestClass
                {
                    private sealed class Prepared
                    {
                        public Dictionary<string, List<object?>> ValuesByFile { get; } =
                            new(StringComparer.Ordinal);
                    }

                    /// <summary>
                    /// Combines a sequence that may contain null.
                    /// </summary>
                    public static void M()
                    {
                        Prepared prepared = new();

                        List<object?> baseline = new();
                        baseline.Add(null);

                        prepared.ValuesByFile["a"] = baseline;

                        List<object?> combined = new();

                        if (prepared.ValuesByFile.TryGetValue("a", out List<object?>? stored))
                        {
                            combined.AddRange(stored);
                        }

                        Consume(combined);
                    }

                    private static void Consume(IReadOnlyList<object?> values)
                    {
                        foreach (object? value in values)
                        {
                            Validate(value);
                        }
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            AssertTransitiveArgumentNullFinding(source);
        }

        /// <summary>
        /// Ensures mutation through a list alias retrieved from the dictionary
        /// invalidates the stored sequence invariant.
        /// </summary>
        [Fact]
        public void NullAddedThroughTryGetValueAlias_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class TestClass
                {
                    private sealed class Prepared
                    {
                        public Dictionary<string, List<object?>> ValuesByFile { get; } =
                            new(StringComparer.Ordinal);
                    }

                    /// <summary>
                    /// Mutates a previously safe stored sequence.
                    /// </summary>
                    public static void M()
                    {
                        Prepared prepared = new();

                        List<object?> baseline = new();
                        baseline.Add(new object());

                        prepared.ValuesByFile["a"] = baseline;

                        if (prepared.ValuesByFile.TryGetValue("a", out List<object?>? stored))
                        {
                            stored.Add(null);

                            List<object?> combined = new();
                            combined.AddRange(stored);

                            Consume(combined);
                        }
                    }

                    private static void Consume(IReadOnlyList<object?> values)
                    {
                        foreach (object? value in values)
                        {
                            Validate(value);
                        }
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            AssertTransitiveArgumentNullFinding(source);
        }

        /// <summary>
        /// Verifies that no exception-documentation finding is produced.
        /// </summary>
        /// <param name="source">
        /// The source code to analyze.
        /// </param>
        private static void AssertNoFindings(string source)
        {
            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Verifies that a transitive ArgumentNullException finding remains
        /// present.
        /// </summary>
        /// <param name="source">
        /// The source code to analyze.
        /// </param>
        private static void AssertTransitiveArgumentNullFinding(string source)
        {
            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Contains(
                findings,
                finding =>
                    string.Equals(
                        finding.Smell.ID,
                        XmlDocSmells.MissingTransitiveExceptionDocumentation.ID,
                        StringComparison.Ordinal)
                    && finding.Message.Contains(
                        "System.ArgumentNullException",
                        StringComparison.Ordinal));
        }
    }
}
