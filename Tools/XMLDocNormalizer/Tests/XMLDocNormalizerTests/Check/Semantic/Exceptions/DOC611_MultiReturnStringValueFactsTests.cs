using Microsoft.CodeAnalysis;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests string value facts preserved across multiple normal source-method
    /// return paths.
    /// </summary>
    public sealed class DOC611_MultiReturnStringValueFactsTests
    {
        /// <summary>
        /// Ensures that a source method whose normal return paths are all
        /// non-whitespace preserves that fact at its caller.
        /// </summary>
        [Fact]
        public void MultipleReturns_AllNonWhiteSpace_SuppressArgumentException()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(bool useDefault)
                    {
                        Validate(Resolve(useDefault));
                    }

                    private static string Resolve(bool useDefault)
                    {
                        if (useDefault)
                        {
                            return "default.json";
                        }

                        return "custom.json";
                    }

                    private static void Validate(string value)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(value);
                    }
                }
                """;

            AssertArgumentExceptionAbsent(source);
        }

        /// <summary>
        /// Ensures that a non-whitespace fact established by a direct guard is
        /// preserved when returning a stable get-only auto-property.
        /// </summary>
        [Fact]
        public void GuardedGetOnlyPropertyReturn_PreservesNonWhiteSpaceFact()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(Options options)
                    {
                        Validate(Resolve(options));
                    }

                    private static string Resolve(Options options)
                    {
                        if (!string.IsNullOrWhiteSpace(options.OutputPath))
                        {
                            return options.OutputPath;
                        }

                        return "artifacts/default.json";
                    }

                    private static void Validate(string value)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(value);
                    }

                    public sealed class Options
                    {
                        public Options(string? outputPath)
                        {
                            OutputPath = outputPath;
                        }

                        public string? OutputPath { get; }
                    }
                }
                """;

            AssertArgumentExceptionAbsent(source);
        }

        /// <summary>
        /// Ensures that facts of an unchanged local initializer are preserved
        /// when the local is returned.
        /// </summary>
        [Fact]
        public void StableLocalInitializer_ReturnPreservesNonWhiteSpaceFact()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(string? detail)
                    {
                        Validate(Resolve(detail));
                    }

                    private static string Resolve(string? detail)
                    {
                        string fileName =
                            $"{detail}_comparison.json";

                        return fileName;
                    }

                    private static void Validate(string value)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(value);
                    }
                }
                """;

            AssertArgumentExceptionAbsent(source);
        }

        /// <summary>
        /// Ensures that a successful <see cref="System.IO.Path.Combine(string,
        /// string)"/> call remains non-whitespace when one component is proven
        /// non-whitespace.
        /// </summary>
        [Fact]
        public void PathCombine_NonWhiteSpaceComponentPreservesFact()
        {
            const string source =
                """
                using System;
                using System.IO;

                public static class EntryPoint
                {
                    public static void M(string directory)
                    {
                        Validate(Resolve(directory));
                    }

                    private static string Resolve(string directory)
                    {
                        string fileName =
                            $"report_{1}.json";

                        return Path.Combine(
                            directory,
                            fileName);
                    }

                    private static void Validate(string value)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(value);
                    }
                }
                """;

            AssertArgumentExceptionAbsent(source);
        }

        /// <summary>
        /// Ensures that one potentially whitespace normal return path prevents
        /// a non-whitespace fact from being propagated for the entire method.
        /// </summary>
        [Fact]
        public void MultipleReturns_WhitespacePathRemainsReported()
        {
            const string source =
                """
                using System;

                public static class EntryPoint
                {
                    public static void M(
                        bool whitespace)
                    {
                        Validate(Resolve(whitespace));
                    }

                    private static string Resolve(
                        bool whitespace)
                    {
                        if (whitespace)
                        {
                            return " ";
                        }

                        return "valid.json";
                    }

                    private static void Validate(string value)
                    {
                        ArgumentException.ThrowIfNullOrWhiteSpace(value);
                    }
                }
                """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(
                    source,
                    "M");

            INamedTypeSymbol argumentException =
                run.GetRequiredType(
                    "System.ArgumentException");

            Assert.Single(
                run.Result.GetExceptionPaths(
                    argumentException));
        }

        /// <summary>
        /// Ensures that no <see cref="ArgumentException"/> path remains for
        /// the supplied source.
        /// </summary>
        /// <param name="source">
        /// The source to analyze.
        /// </param>
        private static void AssertArgumentExceptionAbsent(
            string source)
        {
            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(
                    source,
                    "M");

            INamedTypeSymbol argumentException =
                run.GetRequiredType(
                    "System.ArgumentException");

            Assert.Empty(
                run.Result.GetExceptionPaths(
                    argumentException));
        }

        /// <summary>
        /// Ensures that recursively nested source invocations used as arguments do
        /// not restart value-source analysis with an independent recursion guard.
        /// </summary>
        [Fact]
        public void RecursiveSourceInvocationArguments_DoNotRecurseIndefinitely()
        {
            const string source =
                """
        using System;

        public static class EntryPoint
        {
            public static void M()
            {
                Validate(Forward(Recursive()));
            }

            private static string Forward(string value)
            {
                return value;
            }

            private static string Recursive()
            {
                return Forward(Recursive());
            }

            private static void Validate(string value)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
            }
        }
        """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(
                    source,
                    "M");

            INamedTypeSymbol argumentException =
                run.GetRequiredType(
                    "System.ArgumentException");

            Assert.Single(
                run.Result.GetExceptionPaths(
                    argumentException));
        }

        /// <summary>
        /// Ensures that source-return facts survive storage in an unchanged local
        /// variable before the value is passed to another method.
        /// </summary>
        [Fact]
        public void SourceInvocationStoredInLocal_PreservesNonWhiteSpaceFact()
        {
            const string source =
                """
        using System;

        public static class EntryPoint
        {
            public static void M(
                bool useDefault)
            {
                string outputPath =
                    Resolve(useDefault);

                Validate(outputPath);
            }

            private static string Resolve(
                bool useDefault)
            {
                if (useDefault)
                {
                    return "default.json";
                }

                return "custom.json";
            }

            private static void Validate(
                string value)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(
                    value);
            }
        }
        """;

            AssertArgumentExceptionAbsent(
                source);
        }

        /// <summary>
        /// Ensures that declaration-initializer facts are not reused after the local
        /// variable has been reassigned.
        /// </summary>
        [Fact]
        public void ReassignedLocal_DoesNotPreserveInitializerFact()
        {
            const string source =
                """
        using System;

        public static class EntryPoint
        {
            public static void M()
            {
                string outputPath =
                    Resolve();

                outputPath = " ";

                Validate(outputPath);
            }

            private static string Resolve()
            {
                return "valid.json";
            }

            private static void Validate(
                string value)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(
                    value);
            }
        }
        """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(
                    source,
                    "M");

            INamedTypeSymbol argumentException =
                run.GetRequiredType(
                    "System.ArgumentException");

            Assert.Single(
                run.Result.GetExceptionPaths(
                    argumentException));
        }

        /// <summary>
        /// Ensures that <see cref="System.IO.Path.ChangeExtension(string?,
        /// string?)"/> preserves non-nullness when its path argument is proven
        /// non-null.
        /// </summary>
        [Fact]
        public void PathChangeExtension_NonNullPathPreservesNonNullFact()
        {
            const string source =
                """
        using System;
        using System.IO;

        public static class EntryPoint
        {
            public static void M()
            {
                string outputPath =
                    ResolveOutputPath();

                string? textOutputPath =
                    Path.ChangeExtension(
                        outputPath,
                        ".txt");

                Validate(textOutputPath);
            }

            private static string ResolveOutputPath()
            {
                return "artifacts/statistics.json";
            }

            private static void Validate(
                string? value)
            {
                ArgumentNullException.ThrowIfNull(
                    value);
            }
        }
        """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(
                    source,
                    "M");

            INamedTypeSymbol argumentNullException =
                run.GetRequiredType(
                    "System.ArgumentNullException");

            Assert.Empty(
                run.Result.GetExceptionPaths(
                    argumentNullException));
        }

        /// <summary>
        /// Ensures that <see cref="System.IO.Path.ChangeExtension(string?,
        /// string?)"/> does not gain a non-null fact when its path argument may be
        /// null.
        /// </summary>
        [Fact]
        public void PathChangeExtension_NullablePathRemainsUnknown()
        {
            const string source =
                """
        using System;
        using System.IO;

        public static class EntryPoint
        {
            public static void M(
                string? outputPath)
            {
                string? textOutputPath =
                    Path.ChangeExtension(
                        outputPath,
                        ".txt");

                Validate(textOutputPath);
            }

            private static void Validate(
                string? value)
            {
                ArgumentNullException.ThrowIfNull(
                    value);
            }
        }
        """;

            ExceptionFlowAnalyzerTestRun run =
                ExceptionFlowAnalyzerTestHelper.AnalyzeTransitively(
                    source,
                    "M");

            INamedTypeSymbol argumentNullException =
                run.GetRequiredType(
                    "System.ArgumentNullException");

            Assert.Single(
                run.Result.GetExceptionPaths(
                    argumentNullException));
        }
    }
}
