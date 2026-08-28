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
