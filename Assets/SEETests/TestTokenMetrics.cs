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
            TokenMetrics.Gather(tokens, out _, out _, out int complexity, out _);
            Assert.AreEqual(expected, complexity);
        }

        private const float tolerance = 0.001f; // Tolerance for float value comparisons.

        /// <summary>
        /// Checks whether the provided code has the expected Halstead metrics.
        /// </summary>
        [Test]
        public void TestCalculateHalsteadMetrics1()
        {
            // Test case for empty code, in case DistinctOperators, DistinctOperands and/or ProgramVocabulary values are zero.
            string emptyCode = "";

            IList<AntlrToken> tokens = AntlrToken.FromString(emptyCode, AntlrLanguage.Plain);
            TokenMetrics.HalsteadMetrics expected = new(DistinctOperators: 0,
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

            TokenMetrics.Gather(tokens, out _, out _, out _, out TokenMetrics.HalsteadMetrics halstead);
            Assert.AreEqual(expected, halstead);
        }

        [Test]
        public void TestCalculateHalsteadMetrics2()
        {
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
            TokenMetrics.Gather(tokens, out _, out _, out _, out TokenMetrics.HalsteadMetrics halstead);

            Assert.AreEqual(expected.DistinctOperators, halstead.DistinctOperators);
            Assert.AreEqual(expected.DistinctOperands, halstead.DistinctOperands);
            Assert.AreEqual(expected.TotalOperators, halstead.TotalOperators);
            Assert.AreEqual(expected.TotalOperands, halstead.TotalOperands);
            Assert.AreEqual(expected.ProgramVocabulary, halstead.ProgramVocabulary);
            Assert.AreEqual(expected.ProgramLength, halstead.ProgramLength);
            Assert.AreEqual(expected.EstimatedProgramLength, halstead.EstimatedProgramLength, tolerance);
            Assert.AreEqual(expected.Volume, halstead.Volume, tolerance);
            Assert.AreEqual(expected.Difficulty, halstead.Difficulty, tolerance);
            Assert.AreEqual(expected.Effort, halstead.Effort, tolerance);
            Assert.AreEqual(expected.TimeRequiredToProgram, halstead.TimeRequiredToProgram, tolerance);
            Assert.AreEqual(expected.NumberOfDeliveredBugs, halstead.NumberOfDeliveredBugs, tolerance);
        }

        [Test]
        public void TestCalculateHalsteadMetrics3()
        {

            // Test case for code with no operators to test Plain Text.
            string code = "This arbitary file has no code.\nJust plain words."; // "." is its own operand.

            IList<AntlrToken> tokens = AntlrToken.FromString(code, AntlrLanguage.Plain);
            TokenMetrics.HalsteadMetrics expected = new(DistinctOperators: 0,
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
            TokenMetrics.Gather(tokens, out _, out _, out _, out TokenMetrics.HalsteadMetrics halstead);

            Assert.AreEqual(expected.DistinctOperators, halstead.DistinctOperators);
            Assert.AreEqual(expected.DistinctOperands, halstead.DistinctOperands);
            Assert.AreEqual(expected.TotalOperators, halstead.TotalOperators);
            Assert.AreEqual(expected.TotalOperands, halstead.TotalOperands);
            Assert.AreEqual(expected.ProgramVocabulary, halstead.ProgramVocabulary);
            Assert.AreEqual(expected.ProgramLength, halstead.ProgramLength);
            Assert.AreEqual(expected.EstimatedProgramLength, halstead.EstimatedProgramLength, tolerance);
            Assert.AreEqual(expected.Volume, halstead.Volume, tolerance);
            Assert.AreEqual(expected.Difficulty, halstead.Difficulty, tolerance);
            Assert.AreEqual(expected.Effort, halstead.Effort, tolerance);
            Assert.AreEqual(expected.TimeRequiredToProgram, halstead.TimeRequiredToProgram, tolerance);
            Assert.AreEqual(expected.NumberOfDeliveredBugs, halstead.NumberOfDeliveredBugs, tolerance);
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
                    ", 9)]
        [TestCase(" ", 0)]
        public void TestCalculateLinesOfCode(string code, int expected)
        {
            IEnumerable<AntlrToken> tokens = AntlrToken.FromString(code, AntlrLanguage.CPP);
            TokenMetrics.Gather(tokens, out TokenMetrics.LineMetrics lineMetrics, out _, out _, out _);
            Assert.AreEqual(expected, lineMetrics.LOC);
        }

        /// <summary>
        /// Checks whether the provided code has the expected number of comments.
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
                    ", 2)]
        [TestCase(" ", 0)]
        public void TestCalculateLinesOfComments(string code, int expected)
        {
            IEnumerable<AntlrToken> tokens = AntlrToken.FromString(code, AntlrLanguage.CPP);
            TokenMetrics.Gather(tokens, out TokenMetrics.LineMetrics lineMetrics, out _, out _, out _);
            Assert.AreEqual(expected, lineMetrics.Comments);
        }

    }
}
