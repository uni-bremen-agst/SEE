using Microsoft.CodeAnalysis;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests non-null return facts derived from source-owned
    /// <c>ConditionalWeakTable.GetValue</c> factories.
    /// </summary>
    public sealed class DOC611_ConditionalWeakTableReturnFactsTests
    {
        /// <summary>
        /// Ensures that an expression lambda which constructs a new value
        /// proves the direct <c>GetValue</c> result non-null.
        /// </summary>
        [Fact]
        public void NonNullExpressionLambda_DirectUse_DoesNotPropagateGuard()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Runtime.CompilerServices;

                public static class EntryPoint
                {
                    private static readonly ConditionalWeakTable<Key, Value> Cache = new();

                    public static void M()
                    {
                        Value value = Cache.GetValue(
                            new Key(),
                            static _ => new Value());

                        Validate(value);
                    }

                    private static void Validate(Value? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }

                    private sealed class Key
                    {
                    }

                    private sealed class Value
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionAbsentInBothTransitiveModes(source);
        }

        /// <summary>
        /// Ensures that a non-null factory fact survives a source wrapper
        /// around <c>GetValue</c>.
        /// </summary>
        [Fact]
        public void NonNullExpressionLambda_WrapperReturn_DoesNotPropagateGuard()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Runtime.CompilerServices;

                public static class EntryPoint
                {
                    private static readonly ConditionalWeakTable<Key, Value> Cache = new();

                    public static void M()
                    {
                        Validate(GetStepValue(new Key()));
                    }

                    private static Value GetStepValue(Key key)
                    {
                        return Cache.GetValue(
                            key,
                            static _ => new Value());
                    }

                    private static void Validate(Value? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }

                    private sealed class Key
                    {
                    }

                    private sealed class Value
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionAbsentInBothTransitiveModes(source);
        }

        /// <summary>
        /// Ensures that all normal returns of a block lambda participate in
        /// the non-null proof.
        /// </summary>
        [Fact]
        public void NonNullBlockLambda_DoesNotPropagateGuard()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Runtime.CompilerServices;

                public static class EntryPoint
                {
                    private static readonly ConditionalWeakTable<Key, Value> Cache = new();

                    public static void M(bool first)
                    {
                        Value value = Cache.GetValue(
                            new Key(),
                            _ =>
                            {
                                if (first)
                                {
                                    return new Value();
                                }

                                return new Value();
                            });

                        Validate(value);
                    }

                    private static void Validate(Value? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }

                    private sealed class Key
                    {
                    }

                    private sealed class Value
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionAbsentInBothTransitiveModes(source);
        }

        /// <summary>
        /// Ensures that one statically resolved source method group can prove
        /// the cached value non-null.
        /// </summary>
        [Fact]
        public void NonNullSourceMethodGroup_DoesNotPropagateGuard()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Runtime.CompilerServices;

                public static class EntryPoint
                {
                    private static readonly ConditionalWeakTable<Key, Value> Cache = new();

                    public static void M()
                    {
                        Value value = Cache.GetValue(
                            new Key(),
                            CreateValue);

                        Validate(value);
                    }

                    private static Value CreateValue(Key key)
                    {
                        return new Value();
                    }

                    private static void Validate(Value? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }

                    private sealed class Key
                    {
                    }

                    private sealed class Value
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionAbsentInBothTransitiveModes(source);
        }

        /// <summary>
        /// Ensures that an anonymous method with a non-null return is safely
        /// supported.
        /// </summary>
        [Fact]
        public void NonNullAnonymousMethod_DoesNotPropagateGuard()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Runtime.CompilerServices;

                public static class EntryPoint
                {
                    private static readonly ConditionalWeakTable<Key, Value> Cache = new();

                    public static void M()
                    {
                        Value value = Cache.GetValue(
                            new Key(),
                            delegate(Key key)
                            {
                                return new Value();
                            });

                        Validate(value);
                    }

                    private static void Validate(Value? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }

                    private sealed class Key
                    {
                    }

                    private sealed class Value
                    {
                    }
                }
                """;

            AssertArgumentNullExceptionAbsentInBothTransitiveModes(source);
        }

        /// <summary>
        /// Ensures that a conditional factory which may return null remains
        /// conservative.
        /// </summary>
        [Fact]
        public void NullableConditionalFactory_PropagatesDownstreamGuard()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Runtime.CompilerServices;

                public static class EntryPoint
                {
                    private static readonly ConditionalWeakTable<Key, Value?> Cache = new();

                    public static void M(bool returnNull)
                    {
                        Value? value = Cache.GetValue(
                            new Key(),
                            _ => returnNull ? null : new Value());

                        Validate(value);
                    }

                    private static void Validate(Value? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }

                    private sealed class Key
                    {
                    }

                    private sealed class Value
                    {
                    }
                }
                """;

            AssertDownstreamArgumentNullExceptionPresentInBothTransitiveModes(source);
        }

        /// <summary>
        /// Ensures that a factory which explicitly returns null cannot create
        /// a non-null result fact.
        /// </summary>
        [Fact]
        public void ExplicitNullFactory_PropagatesDownstreamGuard()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Runtime.CompilerServices;

                public static class EntryPoint
                {
                    private static readonly ConditionalWeakTable<Key, Value?> Cache = new();

                    public static void M()
                    {
                        Value? value = Cache.GetValue(
                            new Key(),
                            static _ => null);

                        Validate(value);
                    }

                    private static void Validate(Value? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }

                    private sealed class Key
                    {
                    }

                    private sealed class Value
                    {
                    }
                }
                """;

            AssertDownstreamArgumentNullExceptionPresentInBothTransitiveModes(source);
        }

        /// <summary>
        /// Ensures that an unresolved callback parameter remains
        /// conservative.
        /// </summary>
        [Fact]
        public void UnknownCallback_PropagatesDownstreamGuard()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Runtime.CompilerServices;

                public static class EntryPoint
                {
                    private static readonly ConditionalWeakTable<Key, Value?> Cache = new();

                    public static void M(
                        ConditionalWeakTable<Key, Value?>.CreateValueCallback factory)
                    {
                        Value? value = Cache.GetValue(
                            new Key(),
                            factory);

                        Validate(value);
                    }

                    private static void Validate(Value? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }

                    public sealed class Key
                    {
                    }

                    public sealed class Value
                    {
                    }
                }
                """;

            AssertDownstreamArgumentNullExceptionPresentInBothTransitiveModes(source);
        }

        /// <summary>
        /// Ensures that a callback with multiple possible targets remains
        /// conservative when one target returns null.
        /// </summary>
        [Fact]
        public void MultipleCallbackTargets_PropagateDownstreamGuard()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Runtime.CompilerServices;

                public static class EntryPoint
                {
                    private static readonly ConditionalWeakTable<Key, Value?> Cache = new();

                    public static void M(bool returnNull)
                    {
                        ConditionalWeakTable<Key, Value?>.CreateValueCallback factory =
                            returnNull
                                ? CreateNull
                                : CreateValue;

                        Value? value = Cache.GetValue(
                            new Key(),
                            factory);

                        Validate(value);
                    }

                    private static Value? CreateNull(Key key)
                    {
                        return null;
                    }

                    private static Value? CreateValue(Key key)
                    {
                        return new Value();
                    }

                    private static void Validate(Value? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }

                    private sealed class Key
                    {
                    }

                    private sealed class Value
                    {
                    }
                }
                """;

            AssertDownstreamArgumentNullExceptionPresentInBothTransitiveModes(source);
        }

        /// <summary>
        /// Ensures that a user-defined type and method with the framework names
        /// cannot receive the framework return fact.
        /// </summary>
        [Fact]
        public void UserDefinedConditionalWeakTable_DoesNotReceiveFrameworkFact()
        {
            const string source =
                """
                #nullable enable
                using System;

                public static class EntryPoint
                {
                    private static readonly ConditionalWeakTable<Key, Value> Cache = new();

                    public static void M()
                    {
                        Value? value = Cache.GetValue(
                            new Key(),
                            static _ => new Value());

                        Validate(value);
                    }

                    private static void Validate(Value? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }

                    private sealed class ConditionalWeakTable<TKey, TValue>
                    {
                        public TValue? GetValue(TKey key, Func<TKey, TValue> factory)
                        {
                            return default;
                        }
                    }

                    private sealed class Key
                    {
                    }

                    private sealed class Value
                    {
                    }
                }
                """;

            AssertDownstreamArgumentNullExceptionPresentInBothTransitiveModes(source);
        }

        /// <summary>
        /// Ensures that another source use capable of storing null prevents a
        /// non-null fact even when the current factory returns a new object.
        /// </summary>
        [Fact]
        public void ExistingNullableTableWrite_PropagatesDownstreamGuard()
        {
            const string source =
                """
                #nullable enable
                using System;
                using System.Runtime.CompilerServices;

                public static class EntryPoint
                {
                    private static readonly ConditionalWeakTable<Key, Value?> Cache = new();

                    public static void M()
                    {
                        Key key = new();
                        Cache.Add(key, null);

                        Value? value = Cache.GetValue(
                            key,
                            static _ => new Value());

                        Validate(value);
                    }

                    private static void Validate(Value? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }

                    private sealed class Key
                    {
                    }

                    private sealed class Value
                    {
                    }
                }
                """;

            AssertDownstreamArgumentNullExceptionPresentInBothTransitiveModes(source);
        }

        /// <summary>
        /// Verifies that no <see cref="ArgumentNullException"/> path remains in
        /// either transitive analyzer.
        /// </summary>
        /// <param name="source">The complete probe source.</param>
        private static void AssertArgumentNullExceptionAbsentInBothTransitiveModes(
            string source)
        {
            AssertArgumentNullExceptionAbsent(
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(source, "M"));

            AssertArgumentNullExceptionAbsent(
                ExceptionFlowAnalyzerTestHelper.AnalyzeSolutionTransitively(source, "M"));
        }

        /// <summary>
        /// Verifies that a downstream validation guard remains reachable in
        /// both transitive analyzers.
        /// </summary>
        /// <param name="source">The complete probe source.</param>
        private static void AssertDownstreamArgumentNullExceptionPresentInBothTransitiveModes(
            string source)
        {
            AssertDownstreamArgumentNullExceptionPresent(
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(source, "M"));

            AssertDownstreamArgumentNullExceptionPresent(
                ExceptionFlowAnalyzerTestHelper.AnalyzeSolutionTransitively(source, "M"));
        }

        /// <summary>
        /// Verifies that one analysis result contains no
        /// <see cref="ArgumentNullException"/> paths.
        /// </summary>
        /// <param name="run">The completed analyzer test run.</param>
        private static void AssertArgumentNullExceptionAbsent(
            ExceptionFlowAnalyzerTestRun run)
        {
            INamedTypeSymbol argumentNullException =
                run.GetRequiredType("System.ArgumentNullException");

            Assert.Empty(run.Result.GetExceptionPaths(argumentNullException));
        }

        /// <summary>
        /// Verifies that one analysis result retains the downstream validation
        /// path rather than merely another framework null guard.
        /// </summary>
        /// <param name="run">The completed analyzer test run.</param>
        private static void AssertDownstreamArgumentNullExceptionPresent(
            ExceptionFlowAnalyzerTestRun run)
        {
            INamedTypeSymbol argumentNullException =
                run.GetRequiredType("System.ArgumentNullException");

            Assert.NotEmpty(
                run.Result.GetExceptionPaths(argumentNullException));
        }
    }
}
