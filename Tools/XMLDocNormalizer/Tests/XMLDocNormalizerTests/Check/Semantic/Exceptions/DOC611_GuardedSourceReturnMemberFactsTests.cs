using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests stable member facts obtained from guarded local values returned
    /// by directly bound source methods.
    /// </summary>
    public sealed class DOC611_GuardedSourceReturnMemberFactsTests
    {
        /// <summary>
        /// Ensures in project-transitive mode that a guarded returned context
        /// preserves non-null init-only member facts.
        /// </summary>
        [Fact]
        public void GuardedReturnedContext_ProjectTransitive_DoesNotProduceFinding()
        {
            AssertGuardedReturnedContextDoesNotProduceFinding(
                ExceptionAnalysisMode.ProjectTransitive);
        }

        /// <summary>
        /// Ensures in solution-transitive mode that a guarded returned context
        /// preserves non-null init-only member facts.
        /// </summary>
        [Fact]
        public void GuardedReturnedContext_SolutionTransitive_DoesNotProduceFinding()
        {
            AssertGuardedReturnedContextDoesNotProduceFinding(
                ExceptionAnalysisMode.SolutionTransitive);
        }

        /// <summary>
        /// Ensures that stable returned-member facts survive another helper
        /// boundary.
        /// </summary>
        [Fact]
        public void GuardedReturnedContextAcrossSecondCall_DoesNotProduceFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Context
                {
                    public object? Value { get; init; }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Creates and forwards a context.
                    /// </summary>
                    public static void M(object? value)
                    {
                        Context? context = Create(value);

                        if (context == null)
                        {
                            return;
                        }

                        Forward(context);
                    }

                    private static Context? Create(object? value)
                    {
                        if (value == null)
                        {
                            return null;
                        }

                        return new Context
                        {
                            Value = value
                        };
                    }

                    private static void Forward(Context context)
                    {
                        ValidateContext(context);
                    }

                    private static void ValidateContext(Context context)
                    {
                        Validate(context.Value);
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

            Assert.Empty(findings);
        }

        /// <summary>
        /// Ensures that a nullable member on another non-null return path is
        /// not incorrectly considered non-null.
        /// </summary>
        [Fact]
        public void AlternateNonNullReturnWithoutMemberFact_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Context
                {
                    public object? Value { get; init; }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Validates a member that is nullable on one return path.
                    /// </summary>
                    public static void M(object? value, bool useValue)
                    {
                        Context? context = Create(value, useValue);

                        if (context == null)
                        {
                            return;
                        }

                        ValidateContext(context);
                    }

                    private static Context? Create(object? value, bool useValue)
                    {
                        if (value == null)
                        {
                            return null;
                        }

                        if (useValue)
                        {
                            return new Context
                            {
                                Value = value
                            };
                        }

                        return new Context
                        {
                            Value = null
                        };
                    }

                    private static void ValidateContext(Context context)
                    {
                        Validate(context.Value);
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            AssertArgumentNullFinding(source);
        }

        /// <summary>
        /// Ensures that replacing the guarded local invalidates member
        /// provenance from its declaration initializer.
        /// </summary>
        [Fact]
        public void GuardedLocalReassignedBeforeCall_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Context
                {
                    public object? Value { get; init; }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Replaces a prepared context before validation.
                    /// </summary>
                    public static void M(object? value)
                    {
                        Context? context = Create(value);

                        if (context == null)
                        {
                            return;
                        }

                        context = new Context
                        {
                            Value = null
                        };

                        ValidateContext(context);
                    }

                    private static Context? Create(object? value)
                    {
                        if (value == null)
                        {
                            return null;
                        }

                        return new Context
                        {
                            Value = value
                        };
                    }

                    private static void ValidateContext(Context context)
                    {
                        Validate(context.Value);
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            AssertArgumentNullFinding(source);
        }

        /// <summary>
        /// Ensures that a custom init-only property is not treated as a stable
        /// auto-property.
        /// </summary>
        [Fact]
        public void CustomInitProperty_StillProducesFinding()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Context
                {
                    public object? Value
                    {
                        get => null;
                        init
                        {
                        }
                    }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Validates a custom property.
                    /// </summary>
                    public static void M(object value)
                    {
                        Context context = Create(value);
                        ValidateContext(context);
                    }

                    private static Context Create(object value)
                    {
                        ArgumentNullException.ThrowIfNull(value);

                        return new Context
                        {
                            Value = value
                        };
                    }

                    private static void ValidateContext(Context context)
                    {
                        Validate(context.Value);
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            AssertArgumentNullFinding(source);
        }

        /// <summary>
        /// Verifies successful guarded returned-member propagation.
        /// </summary>
        /// <param name="mode">
        /// The exception-analysis mode to verify.
        /// </param>
        private static void AssertGuardedReturnedContextDoesNotProduceFinding(
            ExceptionAnalysisMode mode)
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class Context
                {
                    public object? Value { get; init; }
                }

                public static class TestClass
                {
                    /// <summary>
                    /// Creates and validates a guarded context.
                    /// </summary>
                    public static void M(object? value)
                    {
                        Context? context = Create(value);

                        if (context == null)
                        {
                            return;
                        }

                        ValidateContext(context);
                    }

                    private static Context? Create(object? value)
                    {
                        if (value == null)
                        {
                            return null;
                        }

                        return new Context
                        {
                            Value = value
                        };
                    }

                    private static void ValidateContext(Context context)
                    {
                        Validate(context.Value);
                    }

                    private static void Validate(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                mode);

            Assert.Empty(findings);
        }

        /// <summary>
        /// Verifies that an ArgumentNullException finding remains present.
        /// </summary>
        /// <param name="source">
        /// The source code to analyze.
        /// </param>
        private static void AssertArgumentNullFinding(string source)
        {
            List<Finding> findings = CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                ExceptionAnalysisMode.ProjectTransitive);

            Assert.Contains(
                findings,
                finding => finding.Message.Contains(
                    "System.ArgumentNullException",
                    StringComparison.Ordinal));
        }
    }
}
