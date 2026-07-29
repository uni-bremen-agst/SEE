using XMLDocNormalizer.Checks.Infrastructure.Exception.Flow;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests thrown variables, factory results, nullable throw expressions,
    /// and catch rethrows in exception-flow summary graphs.
    /// </summary>
    public sealed class ExceptionFlowSummaryGraphThrowTests
    {
        /// <summary>
        /// Ensures that a directly thrown exception variable contributes its
        /// static exception type.
        /// </summary>
        [Fact]
        public void ThrowVariable_AddsStaticExceptionType()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    public void M(
                        InvalidOperationException exception)
                    {
                        throw exception;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummarySource sourceEntry =
                Assert.Single(
                    run.RootSummary.Sources);

            Assert.Equal(
                "InvalidOperationException",
                sourceEntry.ExceptionType.Name);
        }

        /// <summary>
        /// Ensures that a base-typed thrown variable remains represented by
        /// its statically provable base type.
        /// </summary>
        [Fact]
        public void ThrowBaseVariable_AddsBaseExceptionType()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    public void M(
                        Exception exception)
                    {
                        throw exception;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummarySource sourceEntry =
                Assert.Single(
                    run.RootSummary.Sources);

            Assert.Equal(
                "Exception",
                sourceEntry.ExceptionType.Name);
        }

        /// <summary>
        /// Ensures that throwing a factory result records both the factory
        /// call and the returned exception type.
        /// </summary>
        [Fact]
        public void ThrowFactoryResult_AddsSourceAndFactoryEdge()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        throw CreateException();
                    }

                    private static InvalidOperationException
                        CreateException()
                    {
                        return new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummarySource sourceEntry =
                Assert.Single(
                    run.RootSummary.Sources);

            Assert.Equal(
                "InvalidOperationException",
                sourceEntry.ExceptionType.Name);

            ExceptionFlowSummaryCallEdge factoryEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            Assert.Equal(
                "CreateException",
                factoryEdge.Target.Symbol.Name);
        }

        /// <summary>
        /// Ensures that exceptions thrown while evaluating a factory remain
        /// separate from the exception returned and subsequently thrown.
        /// </summary>
        [Fact]
        public void ThrowFactoryThatMayThrow_PreservesBothFlows()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    public void M(
                        bool fail)
                    {
                        throw CreateException(fail);
                    }

                    private static InvalidOperationException
                        CreateException(
                            bool fail)
                    {
                        if (fail)
                        {
                            throw new ArgumentException();
                        }

                        return new InvalidOperationException();
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Equal(
                "InvalidOperationException",
                Assert.Single(
                    run.RootSummary.Sources)
                    .ExceptionType
                    .Name);

            ExceptionFlowSummaryCallEdge factoryEdge =
                Assert.Single(
                    run.RootSummary.CallEdges);

            ExceptionFlowSummary factorySummary =
                run.GetRequiredSummary(
                    factoryEdge.Target);

            Assert.Equal(
                "ArgumentException",
                Assert.Single(
                    factorySummary.Sources)
                    .ExceptionType
                    .Name);
        }

        /// <summary>
        /// Ensures that a thrown variable inside a throw expression is
        /// analyzed.
        /// </summary>
        [Fact]
        public void ThrowExpressionVariable_AddsExceptionType()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    public string M(
                        string? value,
                        NotSupportedException exception)
                    {
                        return value ??
                               throw exception;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Equal(
                "NotSupportedException",
                Assert.Single(
                    run.RootSummary.Sources)
                    .ExceptionType
                    .Name);
        }

        /// <summary>
        /// Ensures that a parameterless rethrow preserves the original source
        /// without adding a duplicate source.
        /// </summary>
        [Fact]
        public void BareRethrow_PreservesOriginalSourceWithoutDuplicate()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        try
                        {
                            throw new InvalidOperationException();
                        }
                        catch
                        {
                            throw;
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummarySource sourceEntry =
                Assert.Single(
                    run.RootSummary.Sources);

            Assert.Equal(
                "InvalidOperationException",
                sourceEntry.ExceptionType.Name);
        }

        /// <summary>
        /// Ensures that directly throwing the catch variable preserves the
        /// original exception source without adding a duplicate.
        /// </summary>
        [Fact]
        public void CaughtVariableRethrow_PreservesOriginalSource()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        try
                        {
                            throw new ArgumentException();
                        }
                        catch (ArgumentException exception)
                        {
                            throw exception;
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummarySource sourceEntry =
                Assert.Single(
                    run.RootSummary.Sources);

            Assert.Equal(
                "ArgumentException",
                sourceEntry.ExceptionType.Name);
        }

        /// <summary>
        /// Ensures that a throw expression referencing the catch variable is
        /// treated as a rethrow.
        /// </summary>
        [Fact]
        public void CaughtVariableThrowExpression_PreservesOriginalSource()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    public string M(
                        string? value)
                    {
                        try
                        {
                            throw new ArgumentException();
                        }
                        catch (ArgumentException exception)
                        {
                            return value ??
                                   throw exception;
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummarySource sourceEntry =
                Assert.Single(
                    run.RootSummary.Sources);

            Assert.Equal(
                "ArgumentException",
                sourceEntry.ExceptionType.Name);
        }

        /// <summary>
        /// Ensures that an unchanged local alias of the catch variable is
        /// recognized as a rethrow.
        /// </summary>
        [Fact]
        public void StableCaughtVariableAlias_PreservesOriginalSource()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        try
                        {
                            throw new ArgumentException();
                        }
                        catch (ArgumentException exception)
                        {
                            Exception alias =
                                exception;

                            throw alias;
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummarySource sourceEntry =
                Assert.Single(
                    run.RootSummary.Sources);

            Assert.Equal(
                "ArgumentException",
                sourceEntry.ExceptionType.Name);
        }

        /// <summary>
        /// Ensures that a rethrow in a nested catch-clause does not make the
        /// outer catch appear to rethrow its own exception.
        /// </summary>
        [Fact]
        public void NestedCatchRethrow_DoesNotAffectOuterCatch()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        try
                        {
                            throw new ArgumentException();
                        }
                        catch (ArgumentException)
                        {
                            try
                            {
                                throw new InvalidOperationException();
                            }
                            catch (InvalidOperationException)
                            {
                                throw;
                            }
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            ExceptionFlowSummarySource sourceEntry =
                Assert.Single(
                    run.RootSummary.Sources);

            Assert.Equal(
                "InvalidOperationException",
                sourceEntry.ExceptionType.Name);
        }

        /// <summary>
        /// Ensures that a throw inside an uncalled lambda does not prevent the
        /// containing catch-clause from handling its exception.
        /// </summary>
        [Fact]
        public void ThrowInsideUncalledLambda_DoesNotAffectCatchSuppression()
        {
            const string source =
                """
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        try
                        {
                            throw new ArgumentException();
                        }
                        catch (ArgumentException exception)
                        {
                            Action action =
                                () =>
                                {
                                    throw exception;
                                };
                        }
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Empty(
                run.RootSummary.Sources);

            Assert.Empty(
                run.RootSummary.CallEdges);
        }

        /// <summary>
        /// Ensures that throwing the null literal produces
        /// <see cref="NullReferenceException"/>.
        /// </summary>
        [Fact]
        public void ThrowNull_AddsNullReferenceException()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        throw null;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Equal(
                "NullReferenceException",
                Assert.Single(
                    run.RootSummary.Sources)
                    .ExceptionType
                    .Name);
        }

        /// <summary>
        /// Ensures that throwing a default exception reference produces
        /// <see cref="NullReferenceException"/>.
        /// </summary>
        [Fact]
        public void ThrowDefaultException_AddsNullReferenceException()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    public void M()
                    {
                        throw default(Exception);
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Equal(
                "NullReferenceException",
                Assert.Single(
                    run.RootSummary.Sources)
                    .ExceptionType
                    .Name);
        }

        /// <summary>
        /// Ensures that a nullable thrown exception produces both its static
        /// exception type and <see cref="NullReferenceException"/>.
        /// </summary>
        [Fact]
        public void ThrowNullableException_AddsBothPossibleTypes()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    public void M(
                        Exception? exception)
                    {
                        throw exception;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            string[] exceptionNames =
                run.RootSummary.Sources
                    .Select(
                        sourceEntry =>
                            sourceEntry.ExceptionType.Name)
                    .OrderBy(
                        static name =>
                            name,
                        StringComparer.Ordinal)
                    .ToArray();

            Assert.Equal(
                new[]
                {
                    "Exception",
                    "NullReferenceException"
                },
                exceptionNames);
        }

        /// <summary>
        /// Ensures that a constrained generic thrown value contributes its
        /// exception constraint and explicit uncertainty about the runtime
        /// subtype.
        /// </summary>
        [Fact]
        public void ThrowConstrainedTypeParameter_AddsConstraintAndUncertainty()
        {
            const string source =
                """
                #nullable enable
                using System;

                public sealed class TestClass
                {
                    public void M<TException>(
                        TException exception)
                        where TException : Exception
                    {
                        throw exception;
                    }
                }
                """;

            ExceptionFlowSummaryGraphTestRun run =
                ExceptionFlowSummaryGraphTestHelper.Build(
                    source,
                    "M");

            Assert.Equal(
                "Exception",
                Assert.Single(
                    run.RootSummary.Sources)
                    .ExceptionType
                    .Name);

            Assert.Contains(
                run.RootSummary.UncertainTargets,
                target =>
                    target.Contains(
                        "TException",
                        StringComparison.Ordinal));
        }
    }
}
