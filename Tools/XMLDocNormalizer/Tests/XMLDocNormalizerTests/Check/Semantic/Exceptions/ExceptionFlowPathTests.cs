using System.Globalization;
using XMLDocNormalizer.Models;

namespace XMLDocNormalizerTests.Check.Semantic.Exceptions
{
    /// <summary>
    /// Tests stable exception-flow path construction and key serialization.
    /// </summary>
    public sealed class ExceptionFlowPathTests
    {
        /// <summary>
        /// Ensures that the serialized key retains every step field and uses
        /// invariant numeric formatting when the cached fragment is reused.
        /// </summary>
        [Fact]
        public void DeduplicationKey_CachedStepFragmentPreservesExactFormat()
        {
            CultureInfo originalCulture = CultureInfo.CurrentCulture;

            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
                ExceptionFlowPathStep step = new(
                    ExceptionFlowPathStepKind.MethodCall,
                    "Ä",
                    "C.cs",
                    1234,
                    56);

                ExceptionFlowPath firstPath = new(step);
                ExceptionFlowPath secondPath = new(step);

                const string expectedKey = "1:1|1:Ä|4:C.cs|4:1234|2:56|";
                Assert.Equal(expectedKey, firstPath.DeduplicationKey);
                Assert.Equal(expectedKey, secondPath.DeduplicationKey);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        /// <summary>
        /// Ensures that nullable step fields retain their existing empty-part
        /// representation in cached fragments.
        /// </summary>
        [Fact]
        public void DeduplicationKey_CachedStepFragmentPreservesNullFields()
        {
            ExceptionFlowPathStep step = new(
                ExceptionFlowPathStepKind.MethodCall,
                "M",
                null,
                null,
                null);

            ExceptionFlowPath path = new(step);

            Assert.Equal(
                "1:1|1:M|0:|0:|0:|",
                path.DeduplicationKey);
        }

        /// <summary>
        /// Ensures that value-equal but reference-distinct steps produce the
        /// same serialized value without requiring shared object identity.
        /// </summary>
        [Fact]
        public void DeduplicationKey_EquivalentDistinctStepsProduceEqualValues()
        {
            ExceptionFlowPathStep firstStep = new(
                ExceptionFlowPathStepKind.PropertyGetter,
                "Container.Value.get",
                "Container.cs",
                17,
                13);
            ExceptionFlowPathStep secondStep = new(
                ExceptionFlowPathStepKind.PropertyGetter,
                "Container.Value.get",
                "Container.cs",
                17,
                13);

            Assert.NotSame(firstStep, secondStep);
            Assert.Equal(
                new ExceptionFlowPath(firstStep).DeduplicationKey,
                new ExceptionFlowPath(secondStep).DeduplicationKey);
        }

        /// <summary>
        /// Ensures that repeated prepends with one step preserve the complete
        /// key, step order, and step instances.
        /// </summary>
        [Fact]
        public void Prepend_ReusedStepPreservesKeyAndStepOrder()
        {
            ExceptionFlowPathStep terminalStep = new(
                ExceptionFlowPathStepKind.ExplicitThrow,
                "E",
                null,
                null,
                null);
            ExceptionFlowPathStep prefix = new(
                ExceptionFlowPathStepKind.MethodCall,
                "M",
                null,
                null,
                null);
            ExceptionFlowPath suffix = new(terminalStep);
            const string expectedKey =
                "1:1|1:M|0:|0:|0:|2:30|1:E|0:|0:|0:|";

            for (int index = 0; index < 256; index++)
            {
                ExceptionFlowPath path = suffix.Prepend(prefix);

                Assert.Equal(expectedKey, path.DeduplicationKey);
                Assert.Equal(2, path.Steps.Count);
                Assert.Same(prefix, path.Steps[0]);
                Assert.Same(terminalStep, path.Steps[1]);
            }
        }

        /// <summary>
        /// Ensures that a changed record copy receives a key matching its new
        /// values while the original cached value remains unchanged.
        /// </summary>
        [Fact]
        public void DeduplicationKey_WithChangedStepDoesNotReuseStaleValue()
        {
            ExceptionFlowPathStep originalStep = new(
                ExceptionFlowPathStepKind.MethodCall,
                "M",
                "C.cs",
                42,
                9);
            ExceptionFlowPath originalPath = new(originalStep);
            ExceptionFlowPathStep changedStep = originalStep with
            {
                Line = 43,
                Column = null
            };
            ExceptionFlowPath changedPath = new(changedStep);

            Assert.NotSame(originalStep, changedStep);
            Assert.Equal(
                "1:1|1:M|4:C.cs|2:42|1:9|",
                originalPath.DeduplicationKey);
            Assert.Equal(
                "1:1|1:M|4:C.cs|2:43|0:|",
                changedPath.DeduplicationKey);
            Assert.Equal(
                "1:1|1:M|4:C.cs|2:42|1:9|",
                new ExceptionFlowPath(originalStep).DeduplicationKey);
        }

        /// <summary>
        /// Ensures that structural deduplication does not materialize the
        /// complete serialized key until its string value is requested.
        /// </summary>
        [Fact]
        public void StructuralDeduplicationKey_MaterializesStringLazily()
        {
            ExceptionFlowPathStep terminal = new(
                ExceptionFlowPathStepKind.ExplicitThrow,
                "System.InvalidOperationException",
                "C.cs",
                11,
                7);
            ExceptionFlowPathStep prefix = new(
                ExceptionFlowPathStepKind.MethodCall,
                "C.M()",
                "C.cs",
                9,
                5);
            ExceptionFlowPath path = new ExceptionFlowPath(terminal).Prepend(prefix);

            Assert.False(path.StructuralDeduplicationKey.IsMaterialized);
            _ = path.StructuralDeduplicationKey.GetHashCode();
            Assert.False(path.StructuralDeduplicationKey.IsMaterialized);

            Assert.Equal(
                "1:1|5:C.M()|4:C.cs|1:9|1:5|2:30|32:System.InvalidOperationException|4:C.cs|2:11|1:7|",
                path.DeduplicationKey);
            Assert.True(path.StructuralDeduplicationKey.IsMaterialized);
        }

        /// <summary>
        /// Ensures that separately constructed paths with identical logical
        /// steps have equal structural keys and compatible hash codes.
        /// </summary>
        [Fact]
        public void StructuralDeduplicationKey_EquivalentPathsAreEqual()
        {
            ExceptionFlowPath first = CreateTwoStepPath(
                ExceptionFlowPathStepKind.PropertyGetter,
                "C.Value.get",
                "C.cs",
                21,
                13);
            ExceptionFlowPath second = CreateTwoStepPath(
                ExceptionFlowPathStepKind.PropertyGetter,
                "C.Value.get",
                "C.cs",
                21,
                13);

            Assert.NotSame(first, second);
            Assert.Equal(
                first.StructuralDeduplicationKey,
                second.StructuralDeduplicationKey);
            Assert.Equal(
                first.StructuralDeduplicationKey.GetHashCode(),
                second.StructuralDeduplicationKey.GetHashCode());
            Assert.Equal(first.DeduplicationKey, second.DeduplicationKey);

            HashSet<ExceptionFlowPathDeduplicationKey> keys = new();
            Assert.True(keys.Add(first.StructuralDeduplicationKey));
            Assert.False(keys.Add(second.StructuralDeduplicationKey));
        }

        /// <summary>
        /// Ensures that every serialized step field contributes to structural
        /// equality exactly as it contributed to the previous string key.
        /// </summary>
        [Fact]
        public void StructuralDeduplicationKey_DifferentStepFieldsAreUnequal()
        {
            ExceptionFlowPath baseline = CreateTwoStepPath(
                ExceptionFlowPathStepKind.MethodCall,
                "C.M()",
                "C.cs",
                31,
                17);
            ExceptionFlowPath[] variants =
            [
                CreateTwoStepPath(
                    ExceptionFlowPathStepKind.VirtualMethodCall,
                    "C.M()",
                    "C.cs",
                    31,
                    17),
                CreateTwoStepPath(
                    ExceptionFlowPathStepKind.MethodCall,
                    "C.N()",
                    "C.cs",
                    31,
                    17),
                CreateTwoStepPath(
                    ExceptionFlowPathStepKind.MethodCall,
                    "C.M()",
                    "D.cs",
                    31,
                    17),
                CreateTwoStepPath(
                    ExceptionFlowPathStepKind.MethodCall,
                    "C.M()",
                    "C.cs",
                    32,
                    17),
                CreateTwoStepPath(
                    ExceptionFlowPathStepKind.MethodCall,
                    "C.M()",
                    "C.cs",
                    31,
                    18),
                CreateTwoStepPath(
                    ExceptionFlowPathStepKind.MethodCall,
                    "C.M()",
                    null,
                    31,
                    17),
                CreateTwoStepPath(
                    ExceptionFlowPathStepKind.MethodCall,
                    "C.M()",
                    "C.cs",
                    null,
                    17),
                CreateTwoStepPath(
                    ExceptionFlowPathStepKind.MethodCall,
                    "C.M()",
                    "C.cs",
                    31,
                    null)
            ];

            foreach (ExceptionFlowPath variant in variants)
            {
                Assert.NotEqual(
                    baseline.StructuralDeduplicationKey,
                    variant.StructuralDeduplicationKey);
                Assert.NotEqual(
                    baseline.DeduplicationKey,
                    variant.DeduplicationKey);
            }
        }

        /// <summary>
        /// Ensures that null and empty string fields retain the previous
        /// normalization to the same serialized value.
        /// </summary>
        [Fact]
        public void StructuralDeduplicationKey_NullAndEmptyStringsRemainEquivalent()
        {
            ExceptionFlowPathStep nullStep = new(
                ExceptionFlowPathStepKind.MethodCall,
                null!,
                null,
                null,
                null);
            ExceptionFlowPathStep emptyStep = new(
                ExceptionFlowPathStepKind.MethodCall,
                string.Empty,
                string.Empty,
                null,
                null);
            ExceptionFlowPath nullPath = new(nullStep);
            ExceptionFlowPath emptyPath = new(emptyStep);

            Assert.Equal(
                nullPath.StructuralDeduplicationKey,
                emptyPath.StructuralDeduplicationKey);
            Assert.Equal(nullPath.DeduplicationKey, emptyPath.DeduplicationKey);
            Assert.Equal("1:1|0:|0:|0:|0:|", nullPath.DeduplicationKey);
        }

        /// <summary>
        /// Ensures that path order, prefixes, and suffixes remain part of the
        /// exact structural identity.
        /// </summary>
        [Fact]
        public void StructuralDeduplicationKey_PreservesOrderPrefixesAndSuffixes()
        {
            ExceptionFlowPathStep a = CreateStep("A", 1);
            ExceptionFlowPathStep b = CreateStep("B", 2);
            ExceptionFlowPathStep c = CreateStep("C", 3);
            ExceptionFlowPathStep d = CreateStep("D", 4);

            ExceptionFlowPath abc = new ExceptionFlowPath(c).Prepend(b).Prepend(a);
            ExceptionFlowPath acb = new ExceptionFlowPath(b).Prepend(c).Prepend(a);
            ExceptionFlowPath dbc = new ExceptionFlowPath(c).Prepend(b).Prepend(d);
            ExceptionFlowPath abd = new ExceptionFlowPath(d).Prepend(b).Prepend(a);

            Assert.NotEqual(
                abc.StructuralDeduplicationKey,
                acb.StructuralDeduplicationKey);
            Assert.NotEqual(
                abc.StructuralDeduplicationKey,
                dbc.StructuralDeduplicationKey);
            Assert.NotEqual(
                abc.StructuralDeduplicationKey,
                abd.StructuralDeduplicationKey);
        }

        /// <summary>
        /// Ensures that different internal fragment boundaries compare as the
        /// same key when their virtual concatenated character streams match.
        /// </summary>
        [Fact]
        public void StructuralDeduplicationKey_FragmentBoundariesMatchStringSemantics()
        {
            ExceptionFlowPathDeduplicationKey first = CreateStructuralKey("ab", "c");
            ExceptionFlowPathDeduplicationKey second = CreateStructuralKey("a", "bc");
            ExceptionFlowPathDeduplicationKey different = CreateStructuralKey("ab", "d");

            Assert.Equal("abc", first.Value);
            Assert.Equal("abc", second.Value);
            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
            Assert.NotEqual(first, different);
        }

        /// <summary>
        /// Ensures that equal polynomial hashes never replace exact logical
        /// key equality during path deduplication.
        /// </summary>
        [Fact]
        public void StructuralDeduplicationKey_HashCollisionRemainsDistinct()
        {
            ExceptionFlowPathDeduplicationKey first = CreateStructuralKey("Aa");
            ExceptionFlowPathDeduplicationKey second = CreateStructuralKey("BB");

            Assert.Equal(first.GetHashCode(), second.GetHashCode());
            Assert.NotEqual(first, second);

            HashSet<ExceptionFlowPathDeduplicationKey> keys = new();
            Assert.True(keys.Add(first));
            Assert.True(keys.Add(second));
            Assert.Equal(2, keys.Count);
        }

        /// <summary>
        /// Creates a representative two-step path.
        /// </summary>
        /// <param name="kind">The prefix step kind.</param>
        /// <param name="symbolName">The prefix symbol name.</param>
        /// <param name="filePath">The prefix file path.</param>
        /// <param name="line">The prefix line.</param>
        /// <param name="column">The prefix column.</param>
        /// <returns>The constructed path.</returns>
        private static ExceptionFlowPath CreateTwoStepPath(
            ExceptionFlowPathStepKind kind,
            string symbolName,
            string? filePath,
            int? line,
            int? column)
        {
            ExceptionFlowPathStep terminal = new(
                ExceptionFlowPathStepKind.ExplicitThrow,
                "System.Exception",
                "Terminal.cs",
                100,
                3);
            ExceptionFlowPathStep prefix = new(
                kind,
                symbolName,
                filePath,
                line,
                column);

            return new ExceptionFlowPath(terminal).Prepend(prefix);
        }

        /// <summary>
        /// Creates a compact representative path step.
        /// </summary>
        /// <param name="symbolName">The step symbol name.</param>
        /// <param name="line">The step line.</param>
        /// <returns>The constructed path step.</returns>
        private static ExceptionFlowPathStep CreateStep(string symbolName, int line)
        {
            return new ExceptionFlowPathStep(
                ExceptionFlowPathStepKind.MethodCall,
                symbolName,
                "C.cs",
                line,
                1);
        }

        /// <summary>
        /// Creates a structural key from arbitrary fragment boundaries.
        /// </summary>
        /// <param name="fragments">The fragments in logical order.</param>
        /// <returns>The constructed structural key.</returns>
        private static ExceptionFlowPathDeduplicationKey CreateStructuralKey(
            params string[] fragments)
        {
            ExceptionFlowPathDeduplicationKey? key = null;

            for (int index = fragments.Length - 1; index >= 0; index--)
            {
                ExceptionFlowPathDeduplicationKeyFragment fragment = new(fragments[index]);
                key = new ExceptionFlowPathDeduplicationKey(fragment, key);
            }

            return key ?? throw new ArgumentException(
                "At least one fragment is required.",
                nameof(fragments));
        }
    }
}
