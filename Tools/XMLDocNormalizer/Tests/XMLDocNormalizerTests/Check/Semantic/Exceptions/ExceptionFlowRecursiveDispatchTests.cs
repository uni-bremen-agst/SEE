using Microsoft.CodeAnalysis;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests runtime dispatch in the recursive transitive exception-flow
    /// analyzer.
    /// </summary>
    public sealed class ExceptionFlowRecursiveDispatchTests
    {
        /// <summary>
        /// Ensures that implementations of an internal interface are analyzed
        /// completely when all possible implementations belong to the current
        /// project.
        /// </summary>
        [Fact]
        public void InternalInterfaceDispatch_AnalyzesKnownImplementation()
        {
            const string source =
                """
                #nullable enable
                using System;

                internal interface IReporter
                {
                    void Complete(Result? result);
                }

                internal sealed class JsonReporter : IReporter
                {
                    public void Complete(Result? result)
                    {
                        Metrics.From(result);
                    }
                }

                internal sealed class ConsoleReporter : IReporter
                {
                    public void Complete(Result? result)
                    {
                    }
                }

                internal static class Metrics
                {
                    public static void From(Result? result)
                    {
                        ArgumentNullException.ThrowIfNull(result);
                    }
                }

                internal sealed class Result
                {
                }

                internal sealed class EntryPoint
                {
                    public void M(
                        IReporter reporter,
                        Result? result)
                    {
                        reporter.Complete(result);
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeTransitively(
                        source,
                        "M");

            AssertExceptionPresent(
                run,
                "System.ArgumentNullException");

            Assert.Empty(
                run.Result.UncertainTargets);
        }

        /// <summary>
        /// Ensures that value facts are transferred from an interface call to
        /// its concrete runtime implementation.
        /// </summary>
        [Fact]
        public void InternalInterfaceDispatch_TransfersArgumentFacts()
        {
            const string source =
                """
                #nullable enable
                using System;

                internal interface IValidator
                {
                    void Validate(string? value);
                }

                internal sealed class Validator : IValidator
                {
                    public void Validate(string? value)
                    {
                        ArgumentNullException.ThrowIfNull(value);
                    }
                }

                internal sealed class EntryPoint
                {
                    public void M(IValidator validator)
                    {
                        validator.Validate("known");
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeTransitively(
                        source,
                        "M");

            AssertExceptionAbsent(
                run,
                "System.ArgumentNullException");

            Assert.Empty(
                run.Result.UncertainTargets);
        }

        /// <summary>
        /// Ensures that known implementations of a public interface are
        /// analyzed while uncertainty is retained for possible external
        /// implementations.
        /// </summary>
        [Fact]
        public void PublicInterfaceDispatch_RetainsExternalUncertainty()
        {
            const string source =
                """
                using System;

                public interface IService
                {
                    void Execute();
                }

                public sealed class Service : IService
                {
                    public void Execute()
                    {
                        throw new InvalidOperationException();
                    }
                }

                public sealed class EntryPoint
                {
                    public void M(IService service)
                    {
                        service.Execute();
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeTransitively(
                        source,
                        "M");

            AssertExceptionPresent(
                run,
                "System.InvalidOperationException");

            Assert.True(
                run.Result.HasUncertainPaths);
        }

        /// <summary>
        /// Ensures that virtual dispatch inside an internal class hierarchy is
        /// resolved completely within the current project.
        /// </summary>
        [Fact]
        public void InternalVirtualDispatch_AnalyzesKnownOverride()
        {
            const string source =
                """
                using System;

                internal class ServiceBase
                {
                    public virtual void Execute()
                    {
                    }
                }

                internal sealed class Service : ServiceBase
                {
                    public override void Execute()
                    {
                        throw new InvalidOperationException();
                    }
                }

                internal sealed class EntryPoint
                {
                    public void M(ServiceBase service)
                    {
                        service.Execute();
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper
                    .AnalyzeTransitively(
                        source,
                        "M");

            AssertExceptionPresent(
                run,
                "System.InvalidOperationException");

            Assert.Empty(
                run.Result.UncertainTargets);
        }

        /// <summary>
        /// Asserts that one exception type is present in the analysis result.
        /// </summary>
        /// <param name="run">
        /// The completed analyzer run.
        /// </param>
        /// <param name="metadataName">
        /// The full metadata name of the expected exception type.
        /// </param>
        private static void AssertExceptionPresent(
            ExceptionFlowAnalyzerTestRun run,
            string metadataName)
        {
            INamedTypeSymbol exceptionType =
                run.GetRequiredType(metadataName);

            Assert.NotEmpty(
                run.Result.GetExceptionPaths(
                    exceptionType));
        }

        /// <summary>
        /// Asserts that one exception type is absent from the analysis result.
        /// </summary>
        /// <param name="run">
        /// The completed analyzer run.
        /// </param>
        /// <param name="metadataName">
        /// The full metadata name of the unexpected exception type.
        /// </param>
        private static void AssertExceptionAbsent(
            ExceptionFlowAnalyzerTestRun run,
            string metadataName)
        {
            INamedTypeSymbol exceptionType =
                run.GetRequiredType(metadataName);

            Assert.Empty(
                run.Result.GetExceptionPaths(
                    exceptionType));
        }
    }
}
