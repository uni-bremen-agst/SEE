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
        /// Halstead metrics.
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
        /// Line metrics.
        /// </summary>
        /// <param name="LOC">Number of lines of code including empty lines and lines with only comments.</param>
        /// <param name="Comments">Number of lines with a comment.</param>
        public record LineMetrics(
            int LOC,
            int Comments
        );

        /// <summary>
        /// Calculates the <paramref name="halsteadMetrics"/>, <paramref name="lineMetrics"/>,
        /// <paramref name="numberOfTokens"/>, and <paramref name="mccabeComplexity"/> for the
        /// provided <paramref name="tokens"/>.
        /// </summary>
        /// <param name="tokens">the token sequence for which to calculate the metrics</param>
        /// <param name="lineMetrics">line metrics</param>
        /// <param name="halsteadMetrics">Halstead metrics</param>
        /// <param name="mccabeComplexity">McCabe complexity</param>
        /// <param name="numberOfTokens">the number of tokens in the sequence (excluding whitespace,
        /// comment, and newline tokens</param>
        public static void Gather
            (IEnumerable<AntlrToken> tokens,
            out LineMetrics lineMetrics,
            out int numberOfTokens,
            out int mccabeComplexity,
            out HalsteadMetrics halsteadMetrics)
        {
            mccabeComplexity = 1;
            numberOfTokens = 0;

            int comments = 0;
            int LOC = 0;

            // Set of token types which are operands for Halstead.
            HashSet<AntlrTokenType> operandTypes = new()
            {
                AntlrTokenType.Identifier,
                AntlrTokenType.Keyword,
                AntlrTokenType.BranchKeyword,
                AntlrTokenType.NumberLiteral,
                AntlrTokenType.StringLiteral
            };
            // Operands and operators for Halstead metrics.
            HashSet<string> distinctOperands = new();
            HashSet<string> distinctOperators = new();
            int totalNunberOfOperators = 0;
            int totalNumberOfOperands = 0;


            foreach (AntlrToken token in tokens)
            {
                // Line of code counting.
                if (token.TokenType == TokenType.Newline)
                {
                    LOC++;
                }
                else if (token.TokenType == AntlrTokenType.Comment)
                {
                    comments++;
                }
                else if (token.TokenType != TokenType.Whitespace)
                {
                    numberOfTokens++;
                }

                // McCabe cyclomatic complexity counting.
                if (token.TokenType == AntlrTokenType.BranchKeyword)
                {
                    mccabeComplexity++;
                }

                // Halstead metrics calculation.
                // Identify operands.
                if (operandTypes.Contains(token.TokenType))
                {
                    distinctOperands.Add(token.Text);
                    totalNumberOfOperands++;
                }
                else
                {
                    if (!IsWhiteSpace(token))
                    {
                        distinctOperators.Add(token.Text);
                        totalNunberOfOperators++;
                    }
                }
            }

            lineMetrics = new LineMetrics(LOC, comments);

            // Derivative Halstead metrics.
            int programVocabulary = distinctOperators.Count + distinctOperands.Count;
            int programLength = totalNumberOfOperands + totalNunberOfOperators;
            float estimatedProgramLength = distinctOperators.Count == 0
                ? 0 : distinctOperators.Count * Mathf.Log(distinctOperators.Count, 2)
                      + distinctOperands.Count * Mathf.Log(distinctOperands.Count, 2);
            float volume = programVocabulary == 0
                ? 0 : programLength * Mathf.Log(programVocabulary, 2);
            float difficulty = distinctOperands.Count == 0
                ? 0 : distinctOperators.Count / 2.0f * (totalNumberOfOperands / (float)distinctOperands.Count);
            float effort = difficulty * volume;
            // Formula: Time T = effort E / S, where S = Stroud's number of psychological 'moments'
            // per second; typically a figure of 18 is used in Software Science.
            float timeRequiredToProgram = effort / 18.0f;
            // Formula: Bugs B = effort E^(2/3) / 3000 or bugs B = volume V / 3000 are both used.
            // 3000 is an empirical estimate.
            float numberOfDeliveredBugs = volume / 3000.0f;

            halsteadMetrics = new HalsteadMetrics(
                distinctOperators.Count,
                distinctOperands.Count,
                totalNunberOfOperators,
                totalNumberOfOperands,
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
        /// True if <paramref name="token"/> is a whitespace token
        /// (newline, comment, or whitespace).
        /// </summary>
        /// <param name="token">token to be checked.</param>
        /// <returns></returns>
        private static bool IsWhiteSpace(AntlrToken token)
        {
            return token.TokenType == TokenType.Whitespace ||
                   token.TokenType == TokenType.Newline ||
                   token.TokenType == AntlrTokenType.Comment ||
                   token.TokenType == AntlrTokenType.Ignored ||
                   token.TokenType == AntlrTokenType.EOF;
        }
    }
}
