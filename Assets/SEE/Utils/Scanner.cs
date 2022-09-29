using System;
using UnityEngine;

namespace SEE.Utils
{
    public partial class ConfigReader
    {
        /// <summary>
        /// A scanner that transforms input text into tokens.
        /// </summary>
        private class Scanner
        {
            /// <summary>
            /// Constructor. Stores <paramref name="input"/> to be processed and forwards
            /// to next non-whitespace character.
            /// </summary>
            /// <param name="input"></param>
            public Scanner(string input)
            {
                this.input = input;
                index = 0;
                Forward();
            }

            /// <summary>
            /// The input to be scanned.
            /// </summary>
            private string input;
            /// <summary>
            /// Index for the begin of the next token or input.Length if all input was read.
            /// </summary>
            private int index;
            /// <summary>
            /// The number of the currently scanned line. First line has number 1.
            /// </summary>
            private int lineNumber = 1;
            /// <summary>
            /// The number of the currently scanned line. First line has number 1.
            /// </summary>
            public int CurrentLineNumber()
            {
                return lineNumber;
            }
            /// <summary>
            /// The textual image of the current token.
            /// </summary>
            private string tokenValue = "";
            /// <summary>
            /// The textual image of the current token.
            /// </summary>
            public string TokenValue()
            {
                return tokenValue;
            }
            /// <summary>
            /// The current token.
            /// </summary>
            private TokenType currentToken = TokenType.Error;
            /// <summary>
            /// The current token.
            /// </summary>
            /// <returns>current token in input</returns>
            public TokenType CurrentToken()
            {
                return currentToken;
            }

            /// <summary>
            /// Moves <see cref="index"/> forward to next character in <see cref="input"/> that
            /// is not white space.
            /// </summary>
            private void Forward()
            {
                while (index < input.Length && IsWhiteSpace(input[index]))
                {
                    if (input[index] == '\n')
                    {
                        lineNumber++;
                    }
                    index++;
                }
                //Debug.Log($"Next input character {(index < input.Length ? input[index] : '#')}\n");
            }

            /// <summary>
            /// Whether given <paramref name="value"/> is to be considered whitespace.
            /// </summary>
            /// <param name="value">character to be checked</param>
            /// <returns>true if <paramref name="value"/> is whitespace</returns>
            private bool IsWhiteSpace(char value)
            {
                return value == ' '
                    || value == '\n'
                    || value == '\t'
                    || value == '\r';
            }

            /// <summary>
            /// Moves forward to the next token in input.
            /// </summary>
            public void NextToken()
            {
                if (index >= input.Length)
                {
                    tokenValue = "";
                    currentToken = TokenType.EndToken;
                }
                else if (input[index] == AttributeSeparator)
                {
                    tokenValue = AttributeSeparator.ToString();
                    index++;
                    Forward();
                    currentToken = TokenType.AttributeSeparator;
                }
                else if (input[index] == LabelSeparator)
                {
                    tokenValue = LabelSeparator.ToString();
                    index++;
                    Forward();
                    currentToken = TokenType.LabelSeparator;
                }
                else if (input[index] == OpenGroup)
                {
                    tokenValue = OpenGroup.ToString();
                    index++;
                    Forward();
                    currentToken = TokenType.Open;
                }
                else if (input[index] == CloseGroup)
                {
                    tokenValue = CloseGroup.ToString();
                    index++;
                    Forward();
                    currentToken = TokenType.Close;
                }
                else if (input[index] == OpenList)
                {
                    tokenValue = OpenList.ToString();
                    index++;
                    Forward();
                    currentToken = TokenType.OpenList;
                }
                else if (input[index] == CloseList)
                {
                    tokenValue = CloseList.ToString();
                    index++;
                    Forward();
                    currentToken = TokenType.CloseList;
                }
                else if (input[index] == '"')  // scanning a string literal
                {
                    // scanned initial "
                    index++;
                    tokenValue = "";
                    // scan forward until the closing "
                    while (index < input.Length)
                    {
                        // a second "
                        if (input[index] == '"')
                        {
                            // the second " may be the end of the string or an escaping "
                            if (index + 1 < input.Length && input[index + 1] == '"')
                            {
                                // double quote => string continues
                                tokenValue += '"';
                                index += 2; // skip both "
                            }
                            else
                            {
                                // no second escaping quote => string ends
                                index++;
                                Forward();
                                currentToken = TokenType.String;
                                return;
                            }
                        }
                        else
                        {
                            // other character
                            tokenValue += input[index];
                            index++;
                        }
                    }
                    // end of input reached
                    currentToken = TokenType.Error;
                    throw new Exception($"unclosed string in input '{input}'");
                }
                else if (Char.IsLetter(input[index]))
                {
                    tokenValue = "";
                    while (index < input.Length && IsLabelLetter(input[index]))
                    {
                        tokenValue += input[index];
                        index++;
                    }
                    Forward();
                    string lowerTokenValue = tokenValue.ToLower();
                    if (lowerTokenValue == "true")
                    {
                        currentToken = TokenType.True;
                    }
                    else if (lowerTokenValue == "false")
                    {
                        currentToken = TokenType.False;
                    }
                    else if (lowerTokenValue == "infinity")
                    {
                        currentToken = TokenType.Float;
                        // Note: NegativeInfinity is emitted as "-Infinity". Because "-"
                        // is not a letter, we will not be scanning it here. It will be
                        // handled in a different branch.
                    }
                    else
                    {
                        currentToken = TokenType.Label;
                    }
                }
                else if (Char.IsDigit(input[index]) || input[index] == '-' || input[index] == '+') // number (integer or float)
                {
                    // Scanning either an integer or float.
                    // We know from the grammar that a number value must be followed by an AttributeSeparator.
                    int endIndex = index + 1;
                    while (endIndex < input.Length && input[endIndex] != AttributeSeparator)
                    {
                        endIndex++;
                    }
                    // The number is in input[index..endIndex-1].
                    // It may be an integer or float.
                    string value = input.Substring(index, endIndex - index).Trim();
                    if (Int64.TryParse(value, out long _))
                    {
                        tokenValue = value;
                        index = endIndex;
                        currentToken = TokenType.Integer;
                    }
                    else if (TryParseFloat(value, out float _))
                    {
                        // Note: This handles also the case of -Infinity.
                        tokenValue = value;
                        index = endIndex;
                        currentToken = TokenType.Float;
                    }
                    else
                    {
                        currentToken = TokenType.Error;
                        tokenValue = "";
                        throw new Exception($"invalid number {value}");
                    }
                }
                else
                {
                    // assert: index < input.Length
                    Debug.LogError($"Unexpected character: {input[index]}\n");
                    tokenValue = "";
                    currentToken = TokenType.Error;
                    index++;
                }
            }

            /// <summary>
            /// True if <paramref name="c"/> is a character that may be a part of label
            /// (i.e., letter, digit, or underscore _).
            /// </summary>
            /// <param name="c">character to be checked</param>
            /// <returns>true for letter, digit, and underscore</returns>
            private bool IsLabelLetter(char c)
            {
                return Char.IsLetterOrDigit(c) || c == '_';
            }

            /// <summary>
            /// The text on the currently scanned input line.
            /// </summary>
            /// <returns>text on the currently scanned input line</returns>
            internal string CurrentLine()
            {
                int l = 1; // line number
                int i = 0; // scans through input
                while (i < input.Length)
                {
                    if (l == lineNumber)
                    {
                        // current line found
                        string result = "";
                        // scan until end of line or end of input
                        for (int j = i; j < input.Length && input[j] != '\n'; j++)
                        {
                            result += input[j];
                        }
                        return result;
                    }
                    else if (input[i] == '\n')
                    {
                        l++;
                    }
                    i++;
                }
                return "";
            }
        }
    }
}