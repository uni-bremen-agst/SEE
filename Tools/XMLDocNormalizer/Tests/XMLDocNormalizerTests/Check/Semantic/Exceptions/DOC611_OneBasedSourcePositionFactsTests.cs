using Microsoft.CodeAnalysis;
using XMLDocNormalizer.Models;
using XMLDocNormalizerTests.Helpers;

namespace XMLDocNormalizerTests.Check.Semantic.Exception
{
    /// <summary>
    /// Tests exception-flow facts for one-based source positions derived from
    /// Roslyn line spans.
    /// </summary>
    public sealed class DOC611_OneBasedSourcePositionFactsTests
    {
        /// <summary>
        /// Ensures that line and column values derived from the start position
        /// of a non-empty Roslyn text span are proven to be one-based.
        /// </summary>
        [Fact]
        public void NonEmptyRoslynLineSpan_OneBasedCoordinatesDoNotProduceFinding()
        {
            const string source =
                """
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Text;

                public sealed class TestClass
                {
                    /// <summary>Creates a source location.</summary>
                    public void M(SyntaxTree tree, int absolutePosition)
                    {
                        TextSpan span = new(absolutePosition, length: 1);
                        FileLinePositionSpan lineSpan = tree.GetLineSpan(span);

                        int line = lineSpan.StartLinePosition.Line + 1;
                        int column = lineSpan.StartLinePosition.Character + 1;

                        Create(line, column);
                    }

                    private static void Create(int line, int column)
                    {
                        if (line < 1)
                        {
                            throw new System.ArgumentOutOfRangeException(nameof(line));
                        }

                        if (column < 1)
                        {
                            throw new System.ArgumentOutOfRangeException(nameof(column));
                        }
                    }
                }
                """;

            List<Finding> findings = Analyze(source);

            Assert.DoesNotContain(
                findings,
                finding => finding.Context.TargetName == "System.ArgumentOutOfRangeException");
        }

        /// <summary>
        /// Ensures that an unknown integer still keeps a conditional
        /// out-of-range exception reachable.
        /// </summary>
        [Fact]
        public void UnknownInteger_StillProducesFinding()
        {
            const string source =
                """
                public sealed class TestClass
                {
                    /// <summary>Validates a source line.</summary>
                    public void M(int line)
                    {
                        Create(line);
                    }

                    private static void Create(int line)
                    {
                        if (line < 1)
                        {
                            throw new System.ArgumentOutOfRangeException(nameof(line));
                        }
                    }
                }
                """;

            List<Finding> findings =
                CheckAssert.FindSemanticExceptionFindingsForSource(
                    source,
                    ExceptionAnalysisMode.ProjectTransitive);

            Assert.Contains(
                findings,
                finding => finding.Context.TargetName == "System.ArgumentOutOfRangeException");
        }

        /// <summary>
        /// Ensures that an arbitrary LinePosition does not receive the stronger
        /// fact reserved for a source coordinate produced by GetLineSpan.
        /// </summary>
        [Fact]
        public void ArbitraryLinePosition_DoesNotReceiveSourceCoordinateFact()
        {
            const string source =
                """
                using Microsoft.CodeAnalysis.Text;

                public sealed class TestClass
                {
                    /// <summary>Validates an arbitrary line position.</summary>
                    public void M()
                    {
                        LinePosition position = new(int.MaxValue, 0);
                        int line = position.Line + 1;

                        Create(line);
                    }

                    private static void Create(int line)
                    {
                        if (line < 1)
                        {
                            throw new System.ArgumentOutOfRangeException(nameof(line));
                        }
                    }
                }
                """;

            List<Finding> findings = Analyze(source);

            Assert.Contains(
                findings,
                finding => finding.Context.TargetName == "System.ArgumentOutOfRangeException");
        }

        /// <summary>
        /// Ensures that a zero-length TextSpan does not receive the stronger
        /// one-based source-position fact.
        /// </summary>
        [Fact]
        public void ZeroLengthRoslynSpan_DoesNotReceiveOneBasedCoordinateFact()
        {
            const string source =
                """
                using Microsoft.CodeAnalysis;
                using Microsoft.CodeAnalysis.Text;

                public sealed class TestClass
                {
                    /// <summary>Creates a source location.</summary>
                    public void M(SyntaxTree tree, int absolutePosition)
                    {
                        TextSpan span = new(absolutePosition, length: 0);
                        FileLinePositionSpan lineSpan = tree.GetLineSpan(span);
                        int line = lineSpan.StartLinePosition.Line + 1;

                        Create(line);
                    }

                    private static void Create(int line)
                    {
                        if (line < 1)
                        {
                            throw new System.ArgumentOutOfRangeException(nameof(line));
                        }
                    }
                }
                """;

            List<Finding> findings = Analyze(source);

            Assert.Contains(
                findings,
                finding => finding.Context.TargetName == "System.ArgumentOutOfRangeException");
        }

        /// <summary>
        /// Runs semantic exception analysis with the Roslyn assemblies available
        /// to the in-memory compilation.
        /// </summary>
        private static List<Finding> Analyze(string source)
        {
            MetadataReference roslynReference =
                MetadataReference.CreateFromFile(typeof(SyntaxTree).Assembly.Location);

            return CheckAssert.FindSemanticExceptionFindingsForSource(
                source,
                ExceptionAnalysisMode.ProjectTransitive,
                roslynReference);
        }
    }
}
