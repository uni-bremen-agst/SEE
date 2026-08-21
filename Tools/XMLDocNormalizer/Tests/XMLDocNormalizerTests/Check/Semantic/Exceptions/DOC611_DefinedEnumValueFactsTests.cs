using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests propagation of facts proving that enum values originate from the
    /// explicitly declared members of their enum type.
    /// </summary>
    public sealed class DOC611_DefinedEnumValueFactsTests
    {
        /// <summary>
        /// Ensures that a declared enum constant makes an exhaustive switch
        /// fallback throw unreachable.
        /// </summary>
        [Fact]
        public void DeclaredEnumConstant_ExhaustiveFallbackDoesNotProduceFinding()
        {
            const string source =
                """
                public enum Mode
                {
                    First,
                    Second
                }

                public sealed class TestClass
                {
                    /// <summary>Executes the operation.</summary>
                    public void M()
                    {
                        _ = Convert(Mode.First);
                    }

                    private static string Convert(Mode mode)
                    {
                        return mode switch
                        {
                            Mode.First => "first",
                            Mode.Second => "second",
                            _ => throw new System.ArgumentOutOfRangeException(nameof(mode))
                        };
                    }
                }
                """;

            List<Finding> findings =
                Analyze(source);

            Assert.DoesNotContain(
                findings,
                IsArgumentOutOfRangeDoc611);
        }

        /// <summary>
        /// Ensures that an independently supplied enum value keeps an
        /// exhaustive-switch fallback throw reachable because arbitrary
        /// integral values can be converted to enum types.
        /// </summary>
        [Fact]
        public void UnknownEnumParameter_ExhaustiveFallbackStillProducesFinding()
        {
            const string source =
                """
                public enum Mode
                {
                    First,
                    Second
                }

                public sealed class TestClass
                {
                    /// <summary>Executes the operation.</summary>
                    public void M(Mode mode)
                    {
                        _ = Convert(mode);
                    }

                    private static string Convert(Mode mode)
                    {
                        return mode switch
                        {
                            Mode.First => "first",
                            Mode.Second => "second",
                            _ => throw new System.ArgumentOutOfRangeException(nameof(mode))
                        };
                    }
                }
                """;

            List<Finding> findings =
                Analyze(source);

            Assert.Contains(
                findings,
                IsArgumentOutOfRangeDoc611);
        }

        /// <summary>
        /// Ensures that foreach iteration over a source method returning a
        /// fixed enum array preserves the defined-enum-value fact.
        /// </summary>
        [Fact]
        public void FixedEnumSequence_ForeachValuePreservesDefinedEnumFact()
        {
            const string source =
                """
                public enum Mode
                {
                    First,
                    Second
                }

                public sealed class TestClass
                {
                    /// <summary>Executes the operation.</summary>
                    public void M()
                    {
                        System.Collections.Generic.IReadOnlyList<Mode> modes =
                            GetModes();

                        foreach (Mode mode in modes)
                        {
                            _ = Convert(mode);
                        }
                    }

                    private static System.Collections.Generic.IReadOnlyList<Mode> GetModes()
                    {
                        return new[]
                        {
                            Mode.First,
                            Mode.Second
                        };
                    }

                    private static string Convert(Mode mode)
                    {
                        return mode switch
                        {
                            Mode.First => "first",
                            Mode.Second => "second",
                            _ => throw new System.ArgumentOutOfRangeException(nameof(mode))
                        };
                    }
                }
                """;

            List<Finding> findings =
                Analyze(source);

            Assert.DoesNotContain(
                findings,
                IsArgumentOutOfRangeDoc611);
        }

        /// <summary>
        /// Ensures that rotating a proven enum sequence through an initially
        /// empty list preserves the fact for every returned element.
        /// </summary>
        [Fact]
        public void RotatedEnumSequence_PreservesDefinedEnumElements()
        {
            const string source =
                """
                public enum Mode
                {
                    First,
                    Second
                }

                public sealed class TestClass
                {
                    /// <summary>Executes the operation.</summary>
                    public void M(bool rotate)
                    {
                        System.Collections.Generic.IReadOnlyList<Mode> modes =
                            GetModes();

                        System.Collections.Generic.IReadOnlyList<Mode> ordered =
                            GetOrder(modes, rotate);

                        foreach (Mode mode in ordered)
                        {
                            _ = Convert(mode);
                        }
                    }

                    private static System.Collections.Generic.IReadOnlyList<Mode> GetModes()
                    {
                        return new[]
                        {
                            Mode.First,
                            Mode.Second
                        };
                    }

                    private static System.Collections.Generic.IReadOnlyList<Mode> GetOrder(
                        System.Collections.Generic.IReadOnlyList<Mode> modes,
                        bool rotate)
                    {
                        if (!rotate)
                        {
                            return modes;
                        }

                        System.Collections.Generic.List<Mode> result = new();

                        for (int index = 0; index < modes.Count; index++)
                        {
                            result.Add(modes[index]);
                        }

                        return result;
                    }

                    private static string Convert(Mode mode)
                    {
                        return mode switch
                        {
                            Mode.First => "first",
                            Mode.Second => "second",
                            _ => throw new System.ArgumentOutOfRangeException(nameof(mode))
                        };
                    }
                }
                """;

            List<Finding> findings =
                Analyze(source);

            Assert.DoesNotContain(
                findings,
                IsArgumentOutOfRangeDoc611);
        }

        /// <summary>
        /// Ensures that an enum cast whose constant value is not declared by
        /// the enum does not receive the defined-enum-value fact.
        /// </summary>
        [Fact]
        public void UndefinedEnumCast_DoesNotReceiveDefinedEnumFact()
        {
            const string source =
                """
                public enum Mode
                {
                    First,
                    Second
                }

                public sealed class TestClass
                {
                    /// <summary>Executes the operation.</summary>
                    public void M()
                    {
                        Mode mode = (Mode)999;
                        _ = Convert(mode);
                    }

                    private static string Convert(Mode mode)
                    {
                        return mode switch
                        {
                            Mode.First => "first",
                            Mode.Second => "second",
                            _ => throw new System.ArgumentOutOfRangeException(nameof(mode))
                        };
                    }
                }
                """;

            List<Finding> findings =
                Analyze(source);

            Assert.Contains(
                findings,
                IsArgumentOutOfRangeDoc611);
        }

        /// <summary>
        /// Runs project-transitive semantic exception analysis for source text.
        /// </summary>
        /// <param name="source">
        /// The source text to analyze.
        /// </param>
        /// <returns>
        /// The semantic exception findings produced for the source.
        /// </returns>
        private static List<Finding> Analyze(string source)
        {
            return CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Determines whether a finding is a missing transitive
        /// <see cref="ArgumentOutOfRangeException"/> documentation finding.
        /// </summary>
        /// <param name="finding">
        /// The finding to inspect.
        /// </param>
        /// <returns>
        /// <see langword="true"/> for the targeted DOC611 finding; otherwise
        /// <see langword="false"/>.
        /// </returns>
        private static bool IsArgumentOutOfRangeDoc611(Finding finding)
        {
            return finding.Smell.ID ==
                    XmlDocSmells.MissingTransitiveExceptionDocumentation.ID
                && finding.Context.TargetName ==
                    "System.ArgumentOutOfRangeException";
        }
    }
}
