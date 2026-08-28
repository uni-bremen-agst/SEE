using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests immutable element-member invariants of extracted tag-like values.
    /// </summary>
    public sealed class DOC611_ExtractedTagElementInvariantTests
    {
        /// <summary>
        /// Ensures that a constructor-guarded get-only member of a reference
        /// type remains non-null when instances are returned in a sequence.
        /// </summary>
        [Fact]
        public void GuardedClassElementFromReturnedSequence_DoesNotProduceFinding()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public sealed class Tag
                {
                    public Tag(object? element)
                    {
                        ArgumentNullException.ThrowIfNull(element);
                        Element = element;
                    }

                    public object Element { get; }
                }

                public static class TagExtraction
                {
                    public static List<Tag> Extract()
                    {
                        List<Tag> tags = new();
                        object element = new object();

                        tags.Add(
                            new Tag(element));

                        return tags;
                    }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Processes extracted tags.
                    /// </summary>
                    public static void M()
                    {
                        List<Tag> tags =
                            TagExtraction.Extract();

                        BuildTagInfos(tags);
                    }

                    private static void BuildTagInfos(
                        List<Tag> tags)
                    {
                        foreach (Tag tag in tags)
                        {
                            Validate(tag.Element);
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
        /// Ensures that the constructor invariant of a value type is not
        /// incorrectly applied to its default value.
        /// </summary>
        [Fact]
        public void GuardedStructElementWithPossibleDefault_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Collections.Generic;

                public readonly struct Tag
                {
                    public Tag(object? element)
                    {
                        ArgumentNullException.ThrowIfNull(element);
                        Element = element;
                    }

                    public object Element { get; }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Processes supplied tags.
                    /// </summary>
                    public static void M(List<Tag> tags)
                    {
                        foreach (Tag tag in tags)
                        {
                            Validate(tag.Element);
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

            Assert.Contains(
                findings,
                finding =>
                    string.Equals(
                        finding.Smell.ID,
                        XmlDocSmells
                            .MissingTransitiveExceptionDocumentation
                            .ID,
                        StringComparison.Ordinal)
                    && finding.Message.Contains(
                        "System.ArgumentNullException",
                        StringComparison.Ordinal));
        }
    }
}
