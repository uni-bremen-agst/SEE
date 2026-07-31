using Microsoft.CodeAnalysis;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests immutable property invariants used by productive summary-graph
    /// analysis.
    /// </summary>
    public sealed class DOC611_ImmutablePropertyInvariantTests
    {
        /// <summary>
        /// Ensures that an object-creation initializer on a get-only property
        /// establishes a non-null invariant for an implicit constructor.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the focused source cannot be compiled or analyzed.
        /// </exception>
        [Fact]
        public void ImplicitConstructorPropertyInitializer_DoesNotThrow()
        {
            const string source =
                """
                using System;

                public sealed class Frame
                {
                    public object Result { get; } = new object();
                }

                public static class EntryPoint
                {
                    public static void M(Frame frame)
                    {
                        Consume(frame.Result);
                    }

                    private static void Consume(object result)
                    {
                        ArgumentNullException.ThrowIfNull(result);
                    }
                }
                """;

            AssertNoArgumentNullException(
                source);
        }

        /// <summary>
        /// Ensures that an explicit terminal constructor without a property
        /// assignment preserves the declaration initializer's value facts.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the focused source cannot be compiled or analyzed.
        /// </exception>
        [Fact]
        public void ExplicitConstructorPropertyInitializer_DoesNotThrow()
        {
            const string source =
                """
                using System;

                public sealed class Frame
                {
                    public Frame()
                    {
                    }

                    public object Result { get; } = new object();
                }

                public static class EntryPoint
                {
                    public static void M(Frame frame)
                    {
                        Consume(frame.Result);
                    }

                    private static void Consume(object result)
                    {
                        ArgumentNullException.ThrowIfNull(result);
                    }
                }
                """;

            AssertNoArgumentNullException(
                source);
        }

        /// <summary>
        /// Ensures that a property initializer is not reused when a terminal
        /// constructor overwrites the property with an unknown value.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the focused source cannot be compiled or analyzed.
        /// </exception>
        [Fact]
        public void NullableConstructorOverwrite_RemainsReported()
        {
            const string source =
                """
                using System;

                public sealed class Holder
                {
                    public Holder(object? value)
                    {
                        Value = value;
                    }

                    public object? Value { get; } = new object();
                }

                public static class EntryPoint
                {
                    public static void M(Holder holder)
                    {
                        Consume(holder.Value);
                    }

                    private static void Consume(object value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeSolutionTransitively(
                        source,
                        "M");

            INamedTypeSymbol argumentNullException =
                run.GetRequiredType(
                    "System.ArgumentNullException");

            Assert.Single(
                run.Result.GetExceptionPaths(
                    argumentNullException));
        }

        /// <summary>
        /// Ensures that a completed frame's initialized result remains
        /// non-null after retrieval from a dictionary.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the focused source cannot be compiled or analyzed.
        /// </exception>
        [Fact]
        public void CachedFrameResult_DoesNotThrow()
        {
            const string source =
                """
                using System;
                using System.Collections.Generic;

                public sealed class Frame
                {
                    public object Result { get; } = new object();
                }

                public static class EntryPoint
                {
                    public static void M()
                    {
                        Dictionary<int, Frame> completedFrames = new();
                        Frame frame = new();

                        completedFrames.Add(
                            0,
                            frame);

                        if (completedFrames.TryGetValue(
                                0,
                                out Frame? completedFrame) &&
                            completedFrame != null)
                        {
                            Consume(
                                completedFrame.Result);
                        }
                    }

                    private static void Consume(object result)
                    {
                        ArgumentNullException.ThrowIfNull(result);
                    }
                }
                """;

            AssertNoArgumentNullException(
                source);
        }

        /// <summary>
        /// Ensures that solution-transitive analysis finds no escaping
        /// <see cref="ArgumentNullException"/>.
        /// </summary>
        /// <param name="source">
        /// The source code to analyze.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="source"/> is null, empty, or consists
        /// only of white-space characters.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the focused source cannot be compiled or analyzed.
        /// </exception>
        private static void AssertNoArgumentNullException(
            string source)
        {
            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeSolutionTransitively(
                        source,
                        "M");

            INamedTypeSymbol argumentNullException =
                run.GetRequiredType(
                    "System.ArgumentNullException");

            Assert.Empty(
                run.Result.GetExceptionPaths(
                    argumentNullException));
        }
    }
}
