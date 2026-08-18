using Microsoft.CodeAnalysis;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests callable boundaries and reachable local callable traversal in the
    /// recursive transitive exception-flow analyzer.
    /// </summary>
    public sealed class ExceptionFlowRecursiveLocalCallableTests
    {
        /// <summary>
        /// Ensures that an uncalled local function does not contribute
        /// exceptions to its containing method.
        /// </summary>
        [Fact]
        public void UncalledLocalFunction_IsExcludedFromOuterFlow()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        void Local()
                        {
                            throw new InvalidOperationException();
                        }
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(
                    source,
                    "M");

            AssertExceptionAbsent(
                run,
                "System.InvalidOperationException");
        }

        /// <summary>
        /// Ensures that a directly invoked local function contributes its
        /// exceptions to the containing method.
        /// </summary>
        [Fact]
        public void CalledLocalFunction_IsAnalyzedTransitively()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        Local();

                        static void Local()
                        {
                            throw new InvalidOperationException();
                        }
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(
                    source,
                    "M");

            AssertExceptionPresent(
                run,
                "System.InvalidOperationException");
        }

        /// <summary>
        /// Ensures that call-site value facts are transferred to an invoked
        /// local function.
        /// </summary>
        [Fact]
        public void LocalFunctionCall_PropagatesParameterFacts()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        Local("value");

                        static void Local(string? value)
                        {
                            ArgumentNullException.ThrowIfNull(value);
                        }
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(
                    source,
                    "M");

            AssertExceptionAbsent(
                run,
                "System.ArgumentNullException");
        }

        /// <summary>
        /// Ensures that an uncalled lambda body does not contribute
        /// exceptions to its containing method.
        /// </summary>
        [Fact]
        public void UncalledLambda_IsExcludedFromOuterFlow()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        Action action =
                            () => throw new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(
                    source,
                    "M");

            AssertExceptionAbsent(
                run,
                "System.InvalidOperationException");
        }

        /// <summary>
        /// Ensures that a stable local delegate initialized with a lambda is
        /// analyzed when invoked.
        /// </summary>
        [Fact]
        public void InvokedLocalLambda_IsAnalyzedTransitively()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        Action action =
                            () => throw new InvalidOperationException();

                        action();
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(
                    source,
                    "M");

            AssertExceptionPresent(
                run,
                "System.InvalidOperationException");

            Assert.False(
                run.Result.HasUncertainPaths);
        }

        /// <summary>
        /// Ensures that invocation arguments are transferred to parameters of
        /// an invoked lambda.
        /// </summary>
        [Fact]
        public void LambdaInvocation_PropagatesParameterFacts()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        Action<string?> action =
                            value =>
                                ArgumentNullException.ThrowIfNull(value);

                        action("value");
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(
                    source,
                    "M");

            AssertExceptionAbsent(
                run,
                "System.ArgumentNullException");

            Assert.False(
                run.Result.HasUncertainPaths);
        }

        /// <summary>
        /// Ensures that a reassigned local delegate is not treated as having
        /// one proven invocation target.
        /// </summary>
        [Fact]
        public void ReassignedDelegate_RemainsUncertain()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        Action action = First;
                        action = Second;
                        action();

                        static void First()
                        {
                            throw new InvalidOperationException();
                        }

                        static void Second()
                        {
                            throw new NotSupportedException();
                        }
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(
                    source,
                    "M");

            AssertExceptionAbsent(
                run,
                "System.InvalidOperationException");

            AssertExceptionAbsent(
                run,
                "System.NotSupportedException");

            Assert.True(
                run.Result.HasUncertainPaths);
        }

        /// <summary>
        /// Ensures that a lambda passed to another method is not treated as
        /// having executed merely because its syntax is nested in the caller.
        /// </summary>
        [Fact]
        public void PassedLambda_IsNotAttributedToOuterFlow()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        Consume(
                            value => Guard(value));
                    }

                    private static void Consume(
                        Func<object?, bool> predicate)
                    {
                        _ = predicate;
                    }

                    private static bool Guard(object? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                        return true;
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(
                    source,
                    "M");

            AssertExceptionAbsent(
                run,
                "System.ArgumentNullException");
        }

        /// <summary>
        /// Asserts that the specified exception type is absent from one
        /// analyzer result.
        /// </summary>
        /// <param name="run">
        /// The completed analyzer run.
        /// </param>
        /// <param name="metadataName">
        /// The full metadata name of the exception type.
        /// </param>
        private static void AssertExceptionAbsent(
            ExceptionFlowAnalyzerTestRun run,
            string metadataName)
        {
            INamedTypeSymbol exceptionType =
                run.GetRequiredType(metadataName);

            Assert.Empty(
                run.Result.GetExceptionPaths(exceptionType));
        }

        /// <summary>
        /// Asserts that the specified exception type is present in one
        /// analyzer result.
        /// </summary>
        /// <param name="run">
        /// The completed analyzer run.
        /// </param>
        /// <param name="metadataName">
        /// The full metadata name of the exception type.
        /// </param>
        private static void AssertExceptionPresent(
            ExceptionFlowAnalyzerTestRun run,
            string metadataName)
        {
            INamedTypeSymbol exceptionType =
                run.GetRequiredType(metadataName);

            Assert.NotEmpty(
                run.Result.GetExceptionPaths(exceptionType));
        }
    }
}
