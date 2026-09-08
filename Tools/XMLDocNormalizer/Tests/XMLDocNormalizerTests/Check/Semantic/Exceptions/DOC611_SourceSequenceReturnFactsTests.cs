using Microsoft.CodeAnalysis;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests non-null sequence-element facts returned by source methods.
    /// </summary>
    public sealed class DOC611_SourceSequenceReturnFactsTests
    {
        /// <summary>
        /// Ensures that a source helper can return a local list populated from
        /// a call-site value proven non-null.
        /// </summary>
        [Fact]
        public void SourceMethodAddsKnownNonNullArgument_ReturnElementsAreNonNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        foreach (Item item in CreateItems(new Item()))
                        {
                            Validate(item);
                        }
                    }

                    private static IReadOnlyList<Item> CreateItems(Item value)
                    {
                        List<Item> items = new();
                        items.Add(value);
                        return items;
                    }

                    private static void Validate(Item? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionAbsentInBothModes(source);
        }

        /// <summary>
        /// Ensures that a stable property proven non-null by a successful
        /// dereference supplies the callee context used for a sequence return.
        /// </summary>
        [Fact]
        public void SourceMethodAddsDereferencedProperty_ReturnElementsAreNonNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M(Options options)
                    {
                        if (options.Value.EndsWith(".first", StringComparison.Ordinal)
                            || options.Value.EndsWith(".second", StringComparison.Ordinal))
                        {
                            return;
                        }

                        foreach (string item in CreateItems(options.Value))
                        {
                            Validate(item);
                        }
                    }

                    private static IReadOnlyList<string> CreateItems(string value)
                    {
                        List<string> items = new();
                        items.Add(value);
                        return items;
                    }

                    private static void Validate(string? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }

                    public sealed class Options
                    {
                        public Options(string? value)
                        {
                            Value = value;
                        }

                        public string? Value { get; }
                    }
                }
                """;

            AssertArgumentNullExceptionAbsentInBothModes(source);
        }

        /// <summary>
        /// Ensures that an unchanged source-return local preserves its element
        /// fact when consumed inside a later conditional block.
        /// </summary>
        [Fact]
        public void SourceReturnStoredBeforeConditionalUse_RemainsNonNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M(bool validate)
                    {
                        IReadOnlyList<Item> items = CreateItems(new Item());

                        if (validate)
                        {
                            ValidateAll(items);
                        }
                    }

                    private static IReadOnlyList<Item> CreateItems(Item value)
                    {
                        List<Item> items = new();
                        items.Add(value);
                        return items;
                    }

                    private static void ValidateAll(IReadOnlyList<Item> items)
                    {
                        foreach (Item item in items)
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionAbsentInBothModes(source);
        }

        /// <summary>
        /// Ensures that a condition which can mutate a source-return local
        /// invalidates its element fact before a nested use.
        /// </summary>
        [Fact]
        public void ConditionalEntryMutatesSourceReturn_RemainsPotentiallyNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        List<Item?> items = CreateItems(new Item());

                        if (Mutate(items))
                        {
                            ValidateAll(items);
                        }
                    }

                    private static List<Item?> CreateItems(Item value)
                    {
                        List<Item?> items = new();
                        items.Add(value);
                        return items;
                    }

                    private static bool Mutate(List<Item?> items)
                    {
                        items.Add(null);
                        return true;
                    }

                    private static void ValidateAll(IReadOnlyList<Item?> items)
                    {
                        foreach (Item? item in items)
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothModes(source);
        }

        /// <summary>
        /// Ensures that the exact supported framework overload is recognized
        /// as returning non-null path elements.
        /// </summary>
        [Fact]
        public void DirectoryEnumerateFilesExactOverload_ReturnElementsAreNonNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.IO;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        foreach (string file in Directory.EnumerateFiles(
                            ".",
                            "*.cs",
                            SearchOption.AllDirectories))
                        {
                            ArgumentNullException.ThrowIfNull(file);
                        }
                    }
                }
                """;

            AssertArgumentNullExceptionAbsentInBothModes(source);
        }

        /// <summary>
        /// Ensures that source-call context, local list additions, framework
        /// sequence elements, multiple returns, and a second call compose.
        /// </summary>
        [Fact]
        public void SourceReturnComposition_PreservesNonNullElements()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;
                using System.IO;

                public static class EntryPoint
                {
                    public static void M(bool returnEarly)
                    {
                        IReadOnlyList<string> values = CreateValues("known", returnEarly);
                        ValidateAll(values);
                    }

                    private static IReadOnlyList<string> CreateValues(
                        string seed,
                        bool returnEarly)
                    {
                        List<string> values = new();
                        values.Add(seed);

                        if (returnEarly)
                        {
                            return values;
                        }

                        foreach (string file in Directory.EnumerateFiles(
                            ".",
                            "*.cs",
                            SearchOption.AllDirectories))
                        {
                            values.Add(file);
                        }

                        return values;
                    }

                    private static void ValidateAll(IReadOnlyList<string> values)
                    {
                        foreach (string value in values)
                        {
                            ArgumentNullException.ThrowIfNull(value);
                        }
                    }
                }
                """;

            AssertArgumentNullExceptionAbsentInBothModes(source);
        }

        /// <summary>
        /// Ensures that a non-null element fact on a sequence parameter can be
        /// preserved by a directly bound source return.
        /// </summary>
        [Fact]
        public void SourceMethodReturnsKnownSequenceParameter_ReturnElementsAreNonNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;
                using System.Linq;

                public static class EntryPoint
                {
                    public static void M(IEnumerable<object?> candidates)
                    {
                        IEnumerable<Item> filtered = candidates.OfType<Item>();

                        foreach (Item item in Forward(filtered))
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }

                    private static IEnumerable<Item> Forward(IEnumerable<Item> values)
                    {
                        return values;
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionAbsentInBothModes(source);
        }

        /// <summary>
        /// Ensures that every normal source return must preserve the element
        /// guarantee before it is propagated to the caller.
        /// </summary>
        [Fact]
        public void MultipleSafeSourceReturns_ReturnElementsAreNonNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M(bool first)
                    {
                        foreach (Item item in CreateItems(new Item(), first))
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }

                    private static IReadOnlyList<Item> CreateItems(Item value, bool first)
                    {
                        List<Item> firstItems = new();
                        firstItems.Add(value);

                        if (first)
                        {
                            return firstItems;
                        }

                        List<Item> secondItems = new();
                        secondItems.Add(new Item());
                        return secondItems;
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionAbsentInBothModes(source);
        }

        /// <summary>
        /// Ensures that one unsafe normal return prevents propagation of a
        /// non-null element guarantee.
        /// </summary>
        [Fact]
        public void SourceReturnCanContainNull_RemainsPotentiallyNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M(bool unsafeReturn)
                    {
                        foreach (Item? item in CreateItems(new Item(), unsafeReturn))
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }

                    private static IReadOnlyList<Item?> CreateItems(
                        Item value,
                        bool unsafeReturn)
                    {
                        List<Item?> safeItems = new();
                        safeItems.Add(value);

                        if (!unsafeReturn)
                        {
                            return safeItems;
                        }

                        List<Item?> unsafeItems = new();
                        unsafeItems.Add(null);
                        return unsafeItems;
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothModes(source);
        }

        /// <summary>
        /// Ensures that facts from one safe call are not reused by another
        /// call whose corresponding argument is unknown.
        /// </summary>
        [Fact]
        public void SameSourceMethodWithSafeAndUnknownArguments_DoesNotMixContexts()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M(Item? unknown)
                    {
                        ValidateAll(CreateItems(new Item()));
                        ValidateAll(CreateItems(unknown));
                    }

                    private static IReadOnlyList<Item?> CreateItems(Item? value)
                    {
                        List<Item?> items = new();
                        items.Add(value);
                        return items;
                    }

                    private static void ValidateAll(IReadOnlyList<Item?> items)
                    {
                        foreach (Item? item in items)
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }

                    public sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothModes(source);
        }

        /// <summary>
        /// Ensures that a virtual source return is not inferred from only the
        /// statically selected implementation.
        /// </summary>
        [Fact]
        public void VirtualSourceReturn_DoesNotAssumeOneImplementation()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M(Provider provider)
                    {
                        foreach (Item? item in provider.CreateItems())
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }
                }

                public class Provider
                {
                    public virtual IReadOnlyList<Item?> CreateItems()
                    {
                        List<Item?> items = new();
                        items.Add(new Item());
                        return items;
                    }
                }

                public sealed class NullableProvider : Provider
                {
                    public override IReadOnlyList<Item?> CreateItems()
                    {
                        return new Item?[] { null };
                    }
                }

                public sealed class Item
                {
                }
                """;

            AssertArgumentNullExceptionPresentInBothModes(source);
        }

        /// <summary>
        /// Ensures that a similarly named non-framework method receives no
        /// framework sequence-element guarantee.
        /// </summary>
        [Fact]
        public void ForeignEnumerateFilesMethod_IsNotFrameworkModeled()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        foreach (string? file in FakeDirectory.EnumerateFiles(
                            ".",
                            "*.cs",
                            SearchDepth.All))
                        {
                            ArgumentNullException.ThrowIfNull(file);
                        }
                    }
                }

                public enum SearchDepth
                {
                    All
                }

                public static class FakeDirectory
                {
                    public static IEnumerable<string?> EnumerateFiles(
                        string path,
                        string pattern,
                        SearchDepth depth)
                    {
                        yield return null;
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothModes(source);
        }

        /// <summary>
        /// Ensures that an unmodeled overload does not inherit the exact
        /// three-parameter framework contract.
        /// </summary>
        [Fact]
        public void DirectoryEnumerateFilesDifferentOverload_IsNotModeled()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.IO;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        foreach (string file in Directory.EnumerateFiles("."))
                        {
                            ArgumentNullException.ThrowIfNull(file);
                        }
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothModes(source);
        }

        /// <summary>
        /// Ensures that unsafe mutation after a source return invalidates its
        /// earlier non-null element guarantee.
        /// </summary>
        [Fact]
        public void SourceReturnedListMutatedBeforeForeach_RemainsPotentiallyNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        List<Item?> items = CreateItems(new Item());
                        items.Add(null);

                        foreach (Item? item in items)
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }

                    private static List<Item?> CreateItems(Item value)
                    {
                        List<Item?> items = new();
                        items.Add(value);
                        return items;
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothModes(source);
        }

        /// <summary>
        /// Ensures that recursive source-return analysis terminates
        /// conservatively without inventing an element fact.
        /// </summary>
        [Fact]
        public void RecursiveSourceSequenceReturn_RemainsPotentiallyNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        foreach (Item? item in CreateItems())
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }

                    private static IEnumerable<Item?> CreateItems()
                    {
                        return CreateItems();
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothModes(source);
        }

        /// <summary>
        /// Ensures that a directly recursive range source terminates
        /// conservatively without inventing an element fact.
        /// </summary>
        [Fact]
        public void RecursiveAddRangeSourceReturn_RemainsPotentiallyNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        foreach (Item? item in CreateItems())
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }

                    private static IEnumerable<Item?> CreateItems()
                    {
                        List<Item?> items = new();
                        items.AddRange(CreateItems());
                        return items;
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothModes(source);
        }

        /// <summary>
        /// Ensures that mutually recursive source returns whose call arguments
        /// request sequence facts terminate conservatively.
        /// </summary>
        [Fact]
        public void MutuallyRecursiveSourceSequenceArgumentFacts_RemainPotentiallyNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        foreach (Item? item in SourceSequenceA())
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }

                    private static IEnumerable<Item?> SourceSequenceA()
                    {
                        return SourceSequenceB(SourceSequenceA());
                    }

                    private static IEnumerable<Item?> SourceSequenceB(
                        IEnumerable<Item?> values)
                    {
                        return values;
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothModes(source);
        }

        /// <summary>
        /// Ensures that mutually recursive range sources terminate
        /// conservatively without inventing an element fact.
        /// </summary>
        [Fact]
        public void MutuallyRecursiveAddRangeSourceReturns_RemainPotentiallyNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        foreach (Item? item in CreateItemsA())
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }

                    private static IEnumerable<Item?> CreateItemsA()
                    {
                        List<Item?> items = new();
                        items.AddRange(CreateItemsB());
                        return items;
                    }

                    private static IEnumerable<Item?> CreateItemsB()
                    {
                        List<Item?> items = new();
                        items.AddRange(CreateItemsA());
                        return items;
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothModes(source);
        }

        /// <summary>
        /// Ensures that completing one proof does not leave its source method
        /// marked while a later independent proof is evaluated.
        /// </summary>
        [Fact]
        public void SafeSourceMethodCalledTwice_BothCallsPreserveFacts()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        ValidateAll(CreateItems(new Item()));
                        ValidateAll(CreateItems(new Item()));
                    }

                    private static IReadOnlyList<Item> CreateItems(Item value)
                    {
                        List<Item> items = new();
                        items.Add(value);
                        return items;
                    }

                    private static void ValidateAll(IReadOnlyList<Item> items)
                    {
                        foreach (Item item in items)
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionAbsentInBothModes(source);
        }

        /// <summary>
        /// Ensures that an unknown <c>AddRange</c> source prevents a returned
        /// list from receiving a non-null element guarantee.
        /// </summary>
        [Fact]
        public void SourceReturnedListAddsUnknownRange_RemainsPotentiallyNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M(IEnumerable<Item?> unknown)
                    {
                        foreach (Item? item in CreateItems(unknown))
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }

                    private static IReadOnlyList<Item?> CreateItems(
                        IEnumerable<Item?> values)
                    {
                        List<Item?> items = new();
                        items.AddRange(values);
                        return items;
                    }

                    public sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothModes(source);
        }

        /// <summary>
        /// Ensures that <c>AddRange</c> evaluates its source with the callee
        /// context created for the concrete source invocation.
        /// </summary>
        [Fact]
        public void SourceReturnedListAddsKnownRange_ReturnElementsAreNonNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;
                using System.Linq;

                public static class EntryPoint
                {
                    public static void M(IEnumerable<object?> candidates)
                    {
                        IEnumerable<Item> filtered = candidates.OfType<Item>();

                        foreach (Item item in CreateItems(filtered))
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }

                    private static IReadOnlyList<Item> CreateItems(
                        IEnumerable<Item> values)
                    {
                        List<Item> items = new();
                        items.AddRange(values);
                        return items;
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionAbsentInBothModes(source);
        }

        /// <summary>
        /// Ensures that iterator methods are not treated as ordinary source
        /// methods with directly inspectable return expressions.
        /// </summary>
        [Fact]
        public void IteratorSourceCanYieldNull_RemainsPotentiallyNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        foreach (Item? item in CreateItems())
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }

                    private static IEnumerable<Item?> CreateItems()
                    {
                        yield return null;
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothModes(source);
        }

        /// <summary>
        /// Ensures that a null collection-initializer element is not treated
        /// as non-null on a source return.
        /// </summary>
        [Fact]
        public void SourceReturnsCollectionInitializerWithNull_RemainsPotentiallyNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        foreach (Item? item in CreateItems())
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }

                    private static IReadOnlyList<Item?> CreateItems()
                    {
                        return new List<Item?> { null };
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothModes(source);
        }

        /// <summary>
        /// Ensures that an index mutation of a local list prevents a source
        /// return from carrying a stale element guarantee.
        /// </summary>
        [Fact]
        public void SourceListIndexMutation_RemainsPotentiallyNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        foreach (Item? item in CreateItems(new Item()))
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }

                    private static IReadOnlyList<Item?> CreateItems(Item value)
                    {
                        List<Item?> items = new();
                        items.Add(value);
                        items[0] = null;
                        return items;
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothModes(source);
        }

        /// <summary>
        /// Ensures that exposing a local list through an alias prevents a
        /// source return from carrying an unsound element guarantee.
        /// </summary>
        [Fact]
        public void SourceListAliasMutation_RemainsPotentiallyNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        foreach (Item? item in CreateItems(new Item()))
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }

                    private static IReadOnlyList<Item?> CreateItems(Item value)
                    {
                        List<Item?> items = new();
                        items.Add(value);
                        List<Item?> alias = items;
                        alias.Add(null);
                        return items;
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothModes(source);
        }

        /// <summary>
        /// Ensures that passing a local list by reference prevents a source
        /// return from carrying an element guarantee.
        /// </summary>
        [Fact]
        public void SourceListRefEscape_RemainsPotentiallyNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        foreach (Item? item in CreateItems(new Item()))
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }

                    private static IReadOnlyList<Item?> CreateItems(Item value)
                    {
                        List<Item?> items = new();
                        items.Add(value);
                        Mutate(ref items);
                        return items;
                    }

                    private static void Mutate(ref List<Item?> items)
                    {
                        items.Add(null);
                    }

                    private sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothModes(source);
        }

        /// <summary>
        /// Ensures that exposing a local list to unknown code prevents a
        /// source return from carrying an element guarantee.
        /// </summary>
        [Fact]
        public void SourceListPassedToUnknownCode_RemainsPotentiallyNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public static class EntryPoint
                {
                    public static void M(Action<List<Item?>> expose)
                    {
                        foreach (Item? item in CreateItems(new Item(), expose))
                        {
                            ArgumentNullException.ThrowIfNull(item);
                        }
                    }

                    private static IReadOnlyList<Item?> CreateItems(
                        Item value,
                        Action<List<Item?>> expose)
                    {
                        List<Item?> items = new();
                        items.Add(value);
                        expose(items);
                        return items;
                    }

                    public sealed class Item
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothModes(source);
        }

        /// <summary>
        /// Ensures that a non-null element fact does not imply that the
        /// sequence object itself is non-null.
        /// </summary>
        [Fact]
        public void DirectoryElementFact_DoesNotProveSequenceInstanceNonNull()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.IO;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        ArgumentNullException.ThrowIfNull(
                            Directory.EnumerateFiles(
                                ".",
                                "*.cs",
                                SearchOption.AllDirectories));
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothModes(source);
        }

        /// <summary>
        /// Ensures that a foreign type with the same type and method names does
        /// not receive the trusted framework contract.
        /// </summary>
        [Fact]
        public void ForeignDirectoryWithMatchingTypeAndMethodNames_IsNotModeled()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    public static void M()
                    {
                        foreach (string? file in Foreign.System.IO.Directory.EnumerateFiles(
                            ".",
                            "*.cs",
                            System.IO.SearchOption.AllDirectories))
                        {
                            ArgumentNullException.ThrowIfNull(file);
                        }
                    }
                }

                namespace Foreign.System.IO
                {
                    public static class Directory
                    {
                        public static global::System.Collections.Generic.IEnumerable<string?> EnumerateFiles(
                            string path,
                            string pattern,
                            global::System.IO.SearchOption option)
                        {
                            yield return null;
                        }
                    }
                }
                """;

            AssertArgumentNullExceptionPresentInBothModes(source);
        }

        /// <summary>
        /// Asserts that neither transitive mode reports an
        /// <see cref="ArgumentNullException"/> path.
        /// </summary>
        /// <param name="source">The complete source to analyze.</param>
        private static void AssertArgumentNullExceptionAbsentInBothModes(string source)
        {
            ExceptionFlowAnalyzerTestRun projectRun =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(source, "M");

            ExceptionFlowAnalyzerTestRun solutionRun =
                ExceptionFlowAnalyzerTestHelper.AnalyzeSolutionTransitively(source, "M");

            AssertArgumentNullExceptionAbsent(projectRun);
            AssertArgumentNullExceptionAbsent(solutionRun);
        }

        /// <summary>
        /// Asserts that one analyzer run contains no
        /// <see cref="ArgumentNullException"/> path.
        /// </summary>
        /// <param name="run">The completed analyzer run.</param>
        private static void AssertArgumentNullExceptionAbsent(ExceptionFlowAnalyzerTestRun run)
        {
            INamedTypeSymbol argumentNullException =
                run.GetRequiredType("System.ArgumentNullException");

            Assert.Empty(run.Result.GetExceptionPaths(argumentNullException));
        }

        /// <summary>
        /// Asserts that both transitive modes retain an
        /// <see cref="ArgumentNullException"/> path.
        /// </summary>
        /// <param name="source">The complete source to analyze.</param>
        private static void AssertArgumentNullExceptionPresentInBothModes(string source)
        {
            ExceptionFlowAnalyzerTestRun projectRun =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(source, "M");

            ExceptionFlowAnalyzerTestRun solutionRun =
                ExceptionFlowAnalyzerTestHelper.AnalyzeSolutionTransitively(source, "M");

            AssertArgumentNullExceptionPresent(projectRun);
            AssertArgumentNullExceptionPresent(solutionRun);
        }

        /// <summary>
        /// Asserts that one analyzer run contains an
        /// <see cref="ArgumentNullException"/> path.
        /// </summary>
        /// <param name="run">The completed analyzer run.</param>
        private static void AssertArgumentNullExceptionPresent(ExceptionFlowAnalyzerTestRun run)
        {
            INamedTypeSymbol argumentNullException =
                run.GetRequiredType("System.ArgumentNullException");

            Assert.NotEmpty(run.Result.GetExceptionPaths(argumentNullException));
        }
    }
}
