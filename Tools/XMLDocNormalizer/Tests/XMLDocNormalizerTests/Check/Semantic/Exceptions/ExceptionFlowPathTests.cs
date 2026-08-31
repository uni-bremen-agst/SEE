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
    }
}
