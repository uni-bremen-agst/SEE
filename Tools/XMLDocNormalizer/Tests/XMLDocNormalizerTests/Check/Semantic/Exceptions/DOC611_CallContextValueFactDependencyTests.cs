using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Characterizes context-sensitive value facts at recursive and nested
    /// same-compilation call boundaries.
    /// </summary>
    public sealed class DOC611_CallContextValueFactDependencyTests
    {
        /// <summary>
        /// Ensures that facts from the outer caller do not bypass the immediate
        /// caller and leak into a nested callee context.
        /// </summary>
        [Fact]
        public void NestedCall_UsesImmediateCallerFactsOnly()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class TestClass
                {
                    /// <summary>Exercises a nested call context.</summary>
                    public static void M(object? value)
                    {
                        if (value == null)
                        {
                            return;
                        }

                        Forward(value);
                    }

                    private static void Forward(object? value)
                    {
                        _ = value;
                        Validate(null);
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                ExceptionAnalysisMode.ProjectTransitive);

            Finding finding = Assert.Single(findings);
            Assert.Equal(
                XmlDocSmells.MissingTransitiveExceptionDocumentation.ID,
                finding.Smell.ID);
            Assert.Contains(
                "System.ArgumentNullException",
                finding.Message,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// Ensures that a recursive A-to-B-to-A call chain terminates while
        /// preserving the non-null argument fact at every boundary.
        /// </summary>
        [Fact]
        public void RecursiveCall_PreservesFactsAndTerminates()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class TestClass
                {
                    /// <summary>Exercises a recursive call context.</summary>
                    public static void M(object? value)
                    {
                        if (value == null)
                        {
                            return;
                        }

                        A(value, true);
                    }

                    private static void A(object? value, bool recurse)
                    {
                        ArgumentNullException.ThrowIfNull(value);

                        if (recurse)
                        {
                            B(value);
                        }
                    }

                    private static void B(object? value)
                    {
                        A(value, false);
                    }
                }
                """;

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                ExceptionAnalysisMode.ProjectTransitive);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that summaries for two calls to the same method do not merge
        /// a known fact from one call site into an unknown second call site.
        /// </summary>
        [Fact]
        public void MultipleCallSites_KeepDistinctArgumentFacts()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class TestClass
                {
                    /// <summary>Exercises distinct call-site contexts.</summary>
                    public static void M(object? value)
                    {
                        Validate(new object());
                        Validate(value);
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                ExceptionAnalysisMode.ProjectTransitive);

            Finding finding = Assert.Single(findings);
            Assert.Equal(
                XmlDocSmells.MissingTransitiveExceptionDocumentation.ID,
                finding.Smell.ID);
            Assert.Contains(
                "System.ArgumentNullException",
                finding.Message,
                StringComparison.Ordinal);
        }
    }
}
