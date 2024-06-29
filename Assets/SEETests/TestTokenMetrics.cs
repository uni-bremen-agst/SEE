using NUnit.Framework;
using System.Collections.Generic;
using SEE.Scanner.Antlr;

namespace SEE.Scanner
{
    /// <summary>
    /// Test cases for <see cref="TokenMetrics"/>.
    /// </summary>
    internal class TestTokenMetrics
    {
        /// <summary>
        /// Checks whether the provided code has the expected McCabe cyclomatic complexity.
        /// </summary>
        /// <param name="code">The provided code.</param>
        /// <param name="expected">The expected McCabe cyclomatic complexity.</param>
        [Test]
        [TestCase(@"using System;

                    public class Program
                    {
                        public static void Main()
                        {
                            if (true)
                            {
                                //This is a comment. Do not count them in, even if it seems tempting.
                                Console.WriteLine(""Hello, if World!"");
                            }
                            else
                            {
                                //This is another comment. Do look further.
                                Console.WriteLine(""Hello, else World!"");
                            }
                        }
                    }", 2)]
        [TestCase(@"public class NoBranchProgram
                    {
                        public int Add(int a, int b)
                        {
                            return a + b;
                        }
                    }", 1)]
        [TestCase("public class EmptyClass\n                    {\n                    }", 1)]
        [TestCase("public class DoesNotCompile\n{\n break; continue; case 2: while do if else foreach for switch try catch }", 7)]
        public void TestCalculateMcCabeComplexity(string code, int expected)
        {
            IEnumerable<AntlrToken> tokens = AntlrToken.FromString(code, AntlrLanguage.CSharp);
            int complexity = TokenMetrics.CalculateMcCabeComplexity(tokens);
            Assert.AreEqual(expected, complexity);
        }

        /// <summary>
        /// Checks whether the provided code has the expected Halstead metrics.
        /// </summary>
        [Test]
        public void TestCalculateHalsteadMetrics()
        {
            const float tolerance = 0.001f; // Tolerance for float value comparisons.

            // Test case for empty code, in case DistinctOperators, DistinctOperands and/or ProgramVocabulary values are zero.
            string emptyCode = "";

            IList<AntlrToken> tokensEmptyCode = AntlrToken.FromString(emptyCode, AntlrLanguage.Plain);
            TokenMetrics.HalsteadMetrics expectedEmptyCode = new(DistinctOperators: 0,
                                                                 DistinctOperands: 0,
                                                                 TotalOperators: 0,
                                                                 TotalOperands: 0,
                                                                 ProgramVocabulary: 0,
                                                                 ProgramLength: 0,
                                                                 EstimatedProgramLength: 0f,
                                                                 Volume: 0f,
                                                                 Difficulty: 0f,
                                                                 Effort: 0f,
                                                                 TimeRequiredToProgram: 0f,
                                                                 NumberOfDeliveredBugs: 0f);
            TokenMetrics.HalsteadMetrics metricsEmptyCode = TokenMetrics.CalculateHalsteadMetrics(tokensEmptyCode);
            Assert.AreEqual(expectedEmptyCode, metricsEmptyCode);

            // Test case for standard code.
            string code = @"public class Program {

                                // A comment for the sake of it.
                                public static void main(String[] args) {
                                    int x = 5 + 3 * 2;
                                    System.out.println(x);
                                }
                            }";

            IList<AntlrToken> tokens = AntlrToken.FromString(code, AntlrLanguage.Java);
            TokenMetrics.HalsteadMetrics expected = new(DistinctOperators: 11,
                                                        DistinctOperands: 16,
                                                        TotalOperators: 17,
                                                        TotalOperands: 18,
                                                        ProgramVocabulary: 27,
                                                        ProgramLength: 35,
                                                        EstimatedProgramLength: 102.0537f,
                                                        Volume: 166.4211f,
                                                        Difficulty: 6.1875f,
                                                        Effort: 1029.73f,
                                                        TimeRequiredToProgram: 57.20724f,
                                                        NumberOfDeliveredBugs: 0.05547369f);
            TokenMetrics.HalsteadMetrics metrics = TokenMetrics.CalculateHalsteadMetrics(tokens);

            Assert.AreEqual(expected.DistinctOperators, metrics.DistinctOperators);
            Assert.AreEqual(expected.DistinctOperands, metrics.DistinctOperands);
            Assert.AreEqual(expected.TotalOperators, metrics.TotalOperators);
            Assert.AreEqual(expected.TotalOperands, metrics.TotalOperands);
            Assert.AreEqual(expected.ProgramVocabulary, metrics.ProgramVocabulary);
            Assert.AreEqual(expected.ProgramLength, metrics.ProgramLength);
            Assert.AreEqual(expected.EstimatedProgramLength, metrics.EstimatedProgramLength, tolerance);
            Assert.AreEqual(expected.Volume, metrics.Volume, tolerance);
            Assert.AreEqual(expected.Difficulty, metrics.Difficulty, tolerance);
            Assert.AreEqual(expected.Effort, metrics.Effort, tolerance);
            Assert.AreEqual(expected.TimeRequiredToProgram, metrics.TimeRequiredToProgram, tolerance);
            Assert.AreEqual(expected.NumberOfDeliveredBugs, metrics.NumberOfDeliveredBugs, tolerance);

            // Test case for code with no operators to test Plain Text.
            string codeWithNoOperators = "This arbitary file has no code.\nJust plain words."; // "." is its own operand.

            IList<AntlrToken> tokensNoOperators = AntlrToken.FromString(codeWithNoOperators, AntlrLanguage.Plain);
            TokenMetrics.HalsteadMetrics expectedNoOperators = new(DistinctOperators: 0,
                                                                   DistinctOperands: 10,
                                                                   TotalOperators: 0,
                                                                   TotalOperands: 11,
                                                                   ProgramVocabulary: 10,
                                                                   ProgramLength: 11,
                                                                   EstimatedProgramLength: 0f,
                                                                   Volume: 36.54121f,
                                                                   Difficulty: 0f,
                                                                   Effort: 0f,
                                                                   TimeRequiredToProgram: 0f,
                                                                   NumberOfDeliveredBugs: 0.0121804f);
            TokenMetrics.HalsteadMetrics metricsNoOperators = TokenMetrics.CalculateHalsteadMetrics(tokensNoOperators);

            Assert.AreEqual(expectedNoOperators.DistinctOperators, metricsNoOperators.DistinctOperators);
            Assert.AreEqual(expectedNoOperators.DistinctOperands, metricsNoOperators.DistinctOperands);
            Assert.AreEqual(expectedNoOperators.TotalOperators, metricsNoOperators.TotalOperators);
            Assert.AreEqual(expectedNoOperators.TotalOperands, metricsNoOperators.TotalOperands);
            Assert.AreEqual(expectedNoOperators.ProgramVocabulary, metricsNoOperators.ProgramVocabulary);
            Assert.AreEqual(expectedNoOperators.ProgramLength, metricsNoOperators.ProgramLength);
            Assert.AreEqual(expectedNoOperators.EstimatedProgramLength, metricsNoOperators.EstimatedProgramLength, tolerance);
            Assert.AreEqual(expectedNoOperators.Volume, metricsNoOperators.Volume, tolerance);
            Assert.AreEqual(expectedNoOperators.Difficulty, metricsNoOperators.Difficulty, tolerance);
            Assert.AreEqual(expectedNoOperators.Effort, metricsNoOperators.Effort, tolerance);
            Assert.AreEqual(expectedNoOperators.TimeRequiredToProgram, metricsNoOperators.TimeRequiredToProgram, tolerance);
            Assert.AreEqual(expectedNoOperators.NumberOfDeliveredBugs, metricsNoOperators.NumberOfDeliveredBugs, tolerance);
        }

        /// <summary>
        /// Checks whether the provided code has the expected lines of code.
        /// </summary>
        /// <param name="code">The provided code.</param>
        /// <param name="expected">The expected lines of code.</param>
        [Test]
        [TestCase(@"class Program {
                    public:
                        int x;

                        // A comment.
                        void setX(int y) {
                            x = y; // An inline comment.
                        }
                    };
                    ", 7)]
        [TestCase(" ", 0)]
        public void TestCalculateLinesOfCode(string code, int expected)
        {
            IEnumerable<AntlrToken> tokens = AntlrToken.FromString(code, AntlrLanguage.CPP);
            int linesOfCode = TokenMetrics.CalculateLinesOfCode(tokens);
            Assert.AreEqual(expected, linesOfCode);
        }
    }
}
