using System.Collections.Generic;
using System.Linq;
using SEE.Scanner.Antlr;
using UnityEngine;

namespace SEE.Scanner
{
    /// <summary>
    /// Provides metrics calculated based on code tokens.
    /// </summary>
    public static class TokenMetrics
    {
        /// <summary>
        /// Calculates the McCabe cyclomatic complexity for provided code.
        /// </summary>
        /// <param name="tokens">The tokens used for which the complexity should be calculated.</param>
        /// <returns>Returns the McCabe cyclomatic complexity.</returns>
        public static int CalculateMcCabeComplexity(IEnumerable<AntlrToken> tokens)
        {
            int complexity = 1; // Starting complexity for a single method or function.

            // Count decision points (branches).
            complexity += tokens.Count(t => t.TokenType == AntlrTokenType.BranchKeyword);

            return complexity;
        }

        /// <summary>
        /// Helper record to store Halstead metrics.
        /// </summary>
        /// <param name="DistinctOperators">The number of distinct operators in the code.</param>
        /// <param name="DistinctOperands">The number of distinct operands in the code.</param>
        /// <param name="TotalOperators">The total number of operators in the code.</param>
        /// <param name="TotalOperands">The total number of operands in the code.</param>
        /// <param name="ProgramVocabulary">The program vocabulary, i.e. the sum of distinct operators and operands.</param>
        /// <param name="ProgramLength">The program length, i.e. the sum of total operators and operands.</param>
        /// <param name="EstimatedProgramLength">The estimated program length based on the program vocabulary.</param>
        /// <param name="Volume">The program volume, which is a measure of the program's size and complexity.</param>
        /// <param name="Difficulty">The program difficulty, which is a measure of how difficult the code is to understand and modify.</param>
        /// <param name="Effort">The program effort, which is a measure of the effort required to understand and modify the code.</param>
        /// <param name="TimeRequiredToProgram">The estimated time required to program the code, based on the program effort.</param>
        /// <param name="NumberOfDeliveredBugs">The estimated number of delivered bugs in the code, based on the program volume.</param>
        public record HalsteadMetrics(
            int DistinctOperators,
            int DistinctOperands,
            int TotalOperators,
            int TotalOperands,
            int ProgramVocabulary,
            int ProgramLength,
            float EstimatedProgramLength,
            float Volume,
            float Difficulty,
            float Effort,
            float TimeRequiredToProgram,
            float NumberOfDeliveredBugs
        );

        /// <summary>
        /// Calculates the Halstead metrics for provided code.
        /// </summary>
        /// <param name="tokens">The tokens for which the metrics should be calculated.</param>
        /// <returns>Returns the Halstead metrics.</returns>
        public static HalsteadMetrics CalculateHalsteadMetrics(ICollection<AntlrToken> tokens)
        {
            // Set of token types which are operands.
            HashSet<AntlrTokenType> operandTypes = new()
            {
                AntlrTokenType.Identifier,
                AntlrTokenType.Keyword,
                AntlrTokenType.BranchKeyword,
                AntlrTokenType.NumberLiteral,
                AntlrTokenType.StringLiteral
            };

            // Identify operands.
            HashSet<string> operands = new(tokens.Where(t => operandTypes.Contains(t.TokenType)).Select(t => t.Text));

            // Identify operators.
            HashSet<string> operators = new(tokens.Where(t => t.TokenType == AntlrTokenType.Punctuation).Select(t => t.Text));

            // Count the total number of operands and operators.
            int totalOperands = tokens.Count(t => operandTypes.Contains(t.TokenType));
            int totalOperators = tokens.Count(t => t.TokenType == AntlrTokenType.Punctuation);

            // Derivative Halstead metrics.
            int programVocabulary = operators.Count + operands.Count;
            int programLength = totalOperators + totalOperands;
            float estimatedProgramLength = operators.Count == 0 ? 0 : operators.Count * Mathf.Log(operators.Count, 2) + operands.Count * Mathf.Log(operands.Count, 2);
            float volume = programVocabulary == 0 ? 0 : programLength * Mathf.Log(programVocabulary, 2);
            float difficulty = operands.Count == 0 ? 0 : operators.Count / 2.0f * (totalOperands / (float)operands.Count);
            float effort = difficulty * volume;
            float timeRequiredToProgram = effort / 18.0f; // Formula: Time T = effort E / S, where S = Stroud's number of psychological 'moments' per second; typically a figure of 18 is used in Software Science.
            float numberOfDeliveredBugs = volume / 3000.0f; // Formula: Bugs B = effort E^(2/3) / 3000 or bugs B = volume V / 3000 are both used. 3000 is an empirical estimate.

            return new HalsteadMetrics(
                operators.Count,
                operands.Count,
                totalOperators,
                totalOperands,
                programVocabulary,
                programLength,
                estimatedProgramLength,
                volume,
                difficulty,
                effort,
                timeRequiredToProgram,
                numberOfDeliveredBugs
            );
        }

        /// <summary>
        /// Calculates the number of lines of code for the provided token stream, excluding comments.
        /// </summary>
        /// <param name="tokens">The tokens for which the lines of code should be counted.</param>
        /// <returns>Returns the number of lines of code.</returns>
        public static int CalculateLinesOfCode(IEnumerable<AntlrToken> tokens)
        {
            int linesOfCode = 0;
            bool comment = false;

            foreach (AntlrToken token in tokens)
            {
                if (token.TokenType == TokenType.Newline)
                {
                    if (!comment)
                    {
                        linesOfCode++;
                    }
                }
                else if (token.TokenType == AntlrTokenType.Comment)
                {
                    comment = true;
                }
                else if (token.TokenType != TokenType.Whitespace)
                {
                    comment = false;
                }
            }
            return linesOfCode;
        }
    }
}
