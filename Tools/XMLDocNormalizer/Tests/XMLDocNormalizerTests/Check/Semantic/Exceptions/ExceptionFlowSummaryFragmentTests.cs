using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests ownership and merge behavior of exception-flow summary
    /// fragments.
    /// </summary>
    public sealed class ExceptionFlowSummaryFragmentTests
    {
        /// <summary>
        /// Ensures that merging a fragment with itself preserves its existing
        /// content without duplicating or clearing it.
        /// </summary>
        [Fact]
        public void Merge_SameInstance_PreservesContent()
        {
            ExceptionFlowSummaryFragment fragment =
                new();

            fragment.AddUncertainTarget(
                "External.Unknown()");

            fragment.Merge(
                fragment);

            Assert.Single(
                fragment.UncertainTargets);

            Assert.Contains(
                "External.Unknown()",
                fragment.UncertainTargets);
        }
    }
}
