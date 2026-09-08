using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests value provenance required by statistics aggregation paths.
    /// </summary>
    public sealed class DOC611_StatisticsProvenanceTests
    {
        /// <summary>
        /// Ensures a guarded conditional local preserves stable property facts
        /// established by a non-null object initializer branch.
        /// </summary>
        [Fact]
        public void StablePropertyFromConditionalObjectInitializer_DoesNotProduceFinding()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.IO;

                public sealed class Statistics
                {
                    public string? ProjectName { get; init; }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Validates a stable property of a conditionally created
                    /// statistics object.
                    /// </summary>
                    public static void M(string? path, bool enabled)
                    {
                        if (path == null)
                        {
                            return;
                        }

                        Statistics? statistics =
                            enabled
                                ? new Statistics
                                {
                                    ProjectName =
                                        Path.GetFileNameWithoutExtension(path)
                                }
                                : null;

                        if (statistics == null)
                        {
                            return;
                        }

                        Validate(statistics.ProjectName);
                    }

                    private static void Validate(string? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures a positive enclosing null check makes a local receiver
        /// available for stable property-initializer analysis.
        /// </summary>
        [Fact]
        public void StablePropertyInsidePositiveReceiverBranch_DoesNotProduceFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Holder
                {
                    public object? Value { get; init; }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Validates a stable value inside its receiver's positive
                    /// null-check branch.
                    /// </summary>
                    public static void M(bool enabled)
                    {
                        Holder? holder =
                            enabled
                                ? new Holder
                                {
                                    Value = new object()
                                }
                                : null;

                        if (holder != null)
                        {
                            Validate(holder.Value);
                        }
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures a positive receiver fact remains available in a nested
        /// positive branch.
        /// </summary>
        [Fact]
        public void StablePropertyInsideNestedPositiveReceiverBranch_DoesNotProduceFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Holder
                {
                    public object? Value { get; init; }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Validates a stable value in a nested branch.
                    /// </summary>
                    public static void M(bool create, bool use)
                    {
                        Holder? holder =
                            create
                                ? new Holder { Value = new object() }
                                : null;

                        if (holder is not null)
                        {
                            if (use)
                            {
                                Validate(holder.Value);
                            }
                        }
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures existing short-circuit condition facts are consumed when
        /// the complete condition dominates the branch body.
        /// </summary>
        [Fact]
        public void StablePropertyInsidePositiveAndBranch_DoesNotProduceFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Holder
                {
                    public object? Value { get; init; }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Validates a stable value in a short-circuit branch.
                    /// </summary>
                    public static void M(bool create, bool use)
                    {
                        Holder? holder =
                            create
                                ? new Holder { Value = new object() }
                                : null;

                        if (holder != null && use)
                        {
                            Validate(holder.Value);
                        }
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures a positive branch fact does not escape the branch it
        /// dominates.
        /// </summary>
        [Fact]
        public void PositiveReceiverFact_DoesNotEscapeBranch()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class TestClass
                {
                    /// <summary>
                    /// Validates a value after a completed null-check branch.
                    /// </summary>
                    public static void M(object? value)
                    {
                        if (value != null)
                        {
                        }

                        Validate(value);
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
        /// Ensures facts from a true condition are not applied to its else
        /// branch.
        /// </summary>
        [Fact]
        public void PositiveReceiverFact_DoesNotApplyToElseBranch()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class TestClass
                {
                    /// <summary>
                    /// Validates a value in the negative null-check branch.
                    /// </summary>
                    public static void M(object? value)
                    {
                        if (value != null)
                        {
                        }
                        else
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
        /// Ensures reassignment to null invalidates an enclosing positive
        /// branch fact.
        /// </summary>
        [Fact]
        public void PositiveReceiverFact_NullReassignmentStillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class TestClass
                {
                    /// <summary>
                    /// Reassigns a positively checked value to null.
                    /// </summary>
                    public static void M(object? value)
                    {
                        if (value != null)
                        {
                            value = null;
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
        /// Ensures reassignment to an unknown value invalidates an enclosing
        /// positive branch fact.
        /// </summary>
        [Fact]
        public void PositiveReceiverFact_UnknownReassignmentStillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class TestClass
                {
                    /// <summary>
                    /// Reassigns a positively checked value to an unknown value.
                    /// </summary>
                    public static void M(object? value, object? replacement)
                    {
                        if (value != null)
                        {
                            value = replacement;
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
        /// Ensures a possible write in an earlier nested statement invalidates
        /// an enclosing positive branch fact.
        /// </summary>
        [Fact]
        public void PositiveReceiverFact_PriorNestedWriteStillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class TestClass
                {
                    /// <summary>
                    /// May replace a positively checked value in a nested branch.
                    /// </summary>
                    public static void M(object? value, bool replace)
                    {
                        if (value != null)
                        {
                            if (replace)
                            {
                                value = null;
                            }

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
        /// Ensures a ref write invalidates an enclosing positive branch fact.
        /// </summary>
        [Fact]
        public void PositiveReceiverFact_RefWriteStillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class TestClass
                {
                    /// <summary>
                    /// Passes a positively checked value to a ref writer.
                    /// </summary>
                    public static void M(object? value)
                    {
                        if (value != null)
                        {
                            Replace(ref value);
                            Validate(value);
                        }
                    }

                    private static void Replace(ref object? value)
                    {
                        value = null;
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
        /// Ensures a non-null receiver does not imply that its stable nullable
        /// property value is non-null.
        /// </summary>
        [Fact]
        public void StablePropertyInsidePositiveReceiverBranch_NullableValueStillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Holder
                {
                    public object? Value { get; init; }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Validates an unknown stable value on a non-null receiver.
                    /// </summary>
                    public static void M(bool create, object? value)
                    {
                        Holder? holder =
                            create
                                ? new Holder { Value = value }
                                : null;

                        if (holder != null)
                        {
                            Validate(holder.Value);
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
        /// Ensures replacing a receiver invalidates stable property facts from
        /// the original object initializer.
        /// </summary>
        [Fact]
        public void StablePropertyReceiverReassignment_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Holder
                {
                    public object? Value { get; init; }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Replaces a checked receiver before validating its value.
                    /// </summary>
                    public static void M(bool create, object? replacement)
                    {
                        Holder? holder =
                            create
                                ? new Holder { Value = new object() }
                                : null;

                        if (holder != null)
                        {
                            holder = new Holder { Value = replacement };
                            Validate(holder.Value);
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
        /// Ensures a stable source-property fact crosses a helper boundary and
        /// remains available through a framework return and object initializer.
        /// </summary>
        [Fact]
        public void StableSourcePropertyAcrossCallAndObjectInitializer_DoesNotProduceFinding()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.IO;

                public sealed class Options
                {
                    public Options(string? path)
                    {
                        Path = path;
                    }

                    public string? Path { get; }
                }

                public sealed class Statistics
                {
                    public string? ProjectName { get; init; }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Validates a project name derived in a helper from a
                    /// previously dereferenced stable source property.
                    /// </summary>
                    public static void M(Options options, bool enabled)
                    {
                        if (options.Path.EndsWith(".skip", StringComparison.Ordinal))
                        {
                            return;
                        }

                        Collect(options, enabled);
                    }

                    private static void Collect(Options options, bool enabled)
                    {
                        Statistics? statistics =
                            enabled
                                ? new Statistics
                                {
                                    ProjectName =
                                        Path.GetFileNameWithoutExtension(options.Path)
                                }
                                : null;

                        if (statistics == null)
                        {
                            return;
                        }

                        Validate(statistics.ProjectName);
                    }

                    private static void Validate(string? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures mutable properties are not treated as stable merely because
        /// their object initializer assigned a non-null value.
        /// </summary>
        [Fact]
        public void MutablePropertyInitializer_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Statistics
                {
                    public object? Value { get; set; }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Validates a mutable property of a conditionally created
                    /// statistics object.
                    /// </summary>
                    public static void M(bool enabled)
                    {
                        Statistics? statistics =
                            enabled
                                ? new Statistics
                                {
                                    Value = new object()
                                }
                                : null;

                        if (statistics == null)
                        {
                            return;
                        }

                        Validate(statistics.Value);
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
        /// Ensures non-null values of a private readonly dictionary are
        /// propagated to KeyValuePair.Value across a source helper boundary.
        /// </summary>
        [Fact]
        public void PrivateReadonlyDictionaryValues_DoesNotProduceFinding()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;
                using System.Collections.ObjectModel;

                public sealed class Item
                {
                }

                public sealed class Holder
                {
                    private readonly Dictionary<string, Item> items = new();

                    private readonly ReadOnlyDictionary<string, Item>
                        readOnlyItems;

                    public Holder()
                    {
                        readOnlyItems =
                            new ReadOnlyDictionary<string, Item>(items);
                    }

                    public IReadOnlyDictionary<string, Item> Items =>
                        readOnlyItems;

                    public void EnsureItem()
                    {
                        if (!items.TryGetValue("x", out Item? item))
                        {
                            item = new Item();
                            items.Add("x", item);
                        }
                    }

                    public void Merge(Holder other)
                    {
                        MergeCore(items, other.items);
                    }

                    private static void MergeCore(
                        Dictionary<string, Item> target,
                        Dictionary<string, Item> source)
                    {
                        foreach (KeyValuePair<string, Item> pair in source)
                        {
                            if (target.TryGetValue(
                                    pair.Key,
                                    out Item? existing))
                            {
                                Validate(pair.Value);
                                continue;
                            }

                            Item clone = new Item();
                            target.Add(pair.Key, clone);
                        }
                    }

                    private static void Validate(Item? item)
                    {
                        ArgumentNullException.ThrowIfNull(item);
                    }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Merges statistics backed by dictionaries containing
                    /// non-null values.
                    /// </summary>
                    public static void M()
                    {
                        Holder target = new Holder();
                        Holder source = new Holder();

                        source.EnsureItem();
                        target.Merge(source);
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures a possible null insertion prevents propagation of the
        /// dictionary-value fact.
        /// </summary>
        [Fact]
        public void PossibleNullDictionaryInsertion_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public sealed class Item
                {
                }

                public sealed class Holder
                {
                    private readonly Dictionary<string, Item> items = new();

                    public void AddMaybe(Item? item)
                    {
                        items.Add("x", item);
                    }

                    public void Merge(Holder other)
                    {
                        MergeCore(items, other.items);
                    }

                    private static void MergeCore(
                        Dictionary<string, Item> target,
                        Dictionary<string, Item> source)
                    {
                        foreach (KeyValuePair<string, Item> pair in source)
                        {
                            Validate(pair.Value);
                        }
                    }

                    private static void Validate(Item? item)
                    {
                        ArgumentNullException.ThrowIfNull(item);
                    }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Merges statistics after a dictionary may have received
                    /// a null value.
                    /// </summary>
                    public static void M()
                    {
                        Holder target = new Holder();
                        Holder source = new Holder();

                        source.AddMaybe(null);
                        target.Merge(source);
                    }
                }
                """;

            AssertTransitiveArgumentNullFinding(source);
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
