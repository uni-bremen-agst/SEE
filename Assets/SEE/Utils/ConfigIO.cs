using SEE.Game;
using System;
using System.IO;
using UnityEngine;

namespace SEE.Utils
{
    public static class ConfigIO
    {
        /// <summary>
        /// The separator between a label and its value.
        /// </summary>
        private const char LabelSeparator = ':';

        /// <summary>
        /// The separator between attribute specifications.
        /// </summary>
        private const char AttributeSeparator = ';';

        /// <summary>
        /// The opening token for a composite attribute value.
        /// </summary>
        private const char Open = '{';
        /// <summary>
        /// The closing token for a composite attribute value.
        /// </summary>
        private const char Close = '}';

        private enum TokenType
        {
            Label,              // [A-Za-z][A-Za-z0-9_]*
            True,               // true
            False,              // false
            Integer,            // [-+][0-9]+
            Float,              // float number according to syntax specified by FloatStyle and CultureInfo.InvariantCulture
            String,             // '"' <any character> '"' where a double quote " within the string must be escaped by a preceeding double quote "
            Open,               // Open = '{'
            Close,              // Close = '}'
            AttributeSeparator, // AttributeSeparator = ';'
            LabelSeparator,     // LabelSeparator = ':'
            EndToken,           // end of input
            Error               // in case of malformed input
        }

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
            /// The number style acceptable when parsing float numbers.
            /// Implied by System.Globalization.NumberStyles.Float are AllowLeadingWhite, AllowTrailingWhite, 
            /// AllowLeadingSign, AllowDecimalPoint, AllowExponent. We also allow AllowThousands.
            /// </summary>
            private const System.Globalization.NumberStyles FloatStyle = System.Globalization.NumberStyles.Float
                        | System.Globalization.NumberStyles.AllowThousands;

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
                else if (input[index] == Open)
                {
                    tokenValue = Open.ToString();
                    index++;
                    Forward();
                    currentToken = TokenType.Open;
                }
                else if (input[index] == Close)
                {
                    tokenValue = Close.ToString();
                    index++;
                    Forward();
                    currentToken = TokenType.Close;
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
                    if (tokenValue == "true")
                    {
                        currentToken = TokenType.True;
                    }
                    else if (tokenValue == "false")
                    {
                        currentToken = TokenType.False;
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
                    else if (Single.TryParse(s: value, style: FloatStyle, 
                                             System.Globalization.CultureInfo.InvariantCulture, out float _))
                    {
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
        
        /// <summary>
        /// Parses the input configuration.
        /// </summary>
        public class Parser
        {
            /// <summary>
            /// Scanner used to transform the input text into tokens.
            /// </summary>
            private readonly Scanner scanner;

            /// <summary>
            /// Parses <paramref name="input"/> according to the following 
            /// grammar in EBNF:
            ///  Config ::= AttributeSeq EndToken .
            ///  AttributeSeq ::= { Attribute } .
            ///  Attribute ::= Label ':' Value ';' .
            ///  Value ::= Bool | Integer | Float | String | Composite .
            ///  Bool ::= True | False .
            ///  Composite ::= '{' AttributeSeq '}' .
            ///  
            /// Throws an exception if input does not conform to this grammar.
            /// </summary>
            /// <param name="input">input to be parsed</param>
            public static void Parse(string input)
            {
                new Parser(input);
            }

            /// <summary>
            /// Config ::= AttributeSeq EndToken .
            /// </summary>
            /// <param name="input"></param>
            private Parser(string input)
            {
                scanner = new Scanner(input);
                scanner.NextToken();
                if (scanner.CurrentToken() == TokenType.EndToken)
                {
                    // No attribute                
                }
                else
                {
                    ParseAttributeSeq();
                    ExpectToken(TokenType.EndToken);
                }
            }

            /// <summary>
            /// AttributeSeq ::= { Attribute } .
            /// </summary>
            private void ParseAttributeSeq()
            {
                while (scanner.CurrentToken() == TokenType.Label)
                {
                    ParseAttribute();
                }
            }

            /// <summary>
            /// Attribute ::= Label ':' Value ';' .
            /// </summary>
            private void ParseAttribute()
            {
                ParseLabel();
                ExpectToken(TokenType.LabelSeparator);
                ParseValue();
                ExpectToken(TokenType.AttributeSeparator);
            }

            /// <summary>
            /// Label .
            /// </summary>
            private void ParseLabel()
            {
                ExpectToken(TokenType.Label);
            }

            /// <summary>
            /// Value ::= Bool | Integer | Float | String | Composite .
            //  Bool ::= True | False .
            /// </summary>
            private void ParseValue()
            {
                switch (scanner.CurrentToken())
                {
                    case TokenType.True:
                        scanner.NextToken();
                        break;
                    case TokenType.False:
                        scanner.NextToken();
                        break;
                    case TokenType.Float:
                        scanner.NextToken();
                        break;
                    case TokenType.Integer:
                        scanner.NextToken();
                        break;
                    case TokenType.String:
                        scanner.NextToken();
                        break;
                    case TokenType.Open:
                        ParseComposite();
                        break;
                    default:
                        Debug.LogError($"true, false, integer, float or {{ expected. Current token is {scanner.CurrentToken()}.\n");
                        break;
                }
            }

            /// <summary>
            /// Composite ::= '{' AttributeSeq '}' .
            /// </summary>
            private void ParseComposite()
            {
                ExpectToken(TokenType.Open);
                ParseAttributeSeq();
                ExpectToken(TokenType.Close);
            }

            /// <summary>
            /// Checks whether the current token is <paramref name="expected"/>. If not,
            /// an exception will be thrown. Moves to the next token.
            /// </summary>
            /// <param name="expected"></param>
            private void ExpectToken(TokenType expected)
            {
                if (scanner.CurrentToken() != expected)
                {
                    Error($"Line {scanner.CurrentLineNumber()}: '{scanner.CurrentLine()}'. Expected token: {expected}. Current token: {scanner.CurrentToken()}");
                }
                scanner.NextToken();
            }

            /// <summary>
            /// Throws exception with given <paramref name="message"/>.
            /// </summary>
            /// <param name="message"></param>
            private void Error(string message)
            {
                throw new Exception(message);
            }
        }

        // ---------------------------------------------------------
        // Output
        // ---------------------------------------------------------

        private static void SaveLabel(StreamWriter stream, string label)
        {
            stream.Write(label + NiceLabelValueSeparator());
        }

        private static string NiceLabelValueSeparator()
        {
            return " " + LabelSeparator + " ";
        }

        private static void InternalSave(StreamWriter stream, string label, string value, bool newLine)
        {
            SaveLabel(stream, label);
            stream.Write(value + AttributeSeparator);
            if (newLine)
            {
                stream.WriteLine();
            }
        }

        internal static void Save(StreamWriter stream, string label, float value, bool newLine = true)
        {
            InternalSave(stream, label, value.ToString("F8", System.Globalization.CultureInfo.InvariantCulture), newLine);
        }

        //internal static float Get(string stream, string label)
        //{
        //    string value = InternalRead(stream, label);
        //    return float.Parse(value);
        //}

        internal static void Save(StreamWriter stream, string label, string value, bool newLine = true)
        {
            InternalSave(stream, label, "\"" + Escape(value) + "\"", newLine);
        }

        internal static void Save(StreamWriter stream, string label, DataPath path)
        {
            SaveLabel(stream, label);
            NiceLabelValueSeparator();

            BeginGroup(stream);
            Save(stream, "Root", path.Root.ToString(), newLine: false);
            Space(stream);
            Save(stream, "RelativePath", path.RelativePath, newLine: false);
            Space(stream);
            Save(stream, "AbsolutePath", path.AbsolutePath, newLine: false);
            EndGroup(stream);

            stream.WriteLine();
        }

        /// <summary>
        /// Returns <paramref name="value"/> where every quote " has been replaced by a double quote "".
        /// </summary>
        /// <param name="value">the string where " is to be escaped</param>
        /// <returns>replacement of " by ""</returns>
        private static string Escape(string value)
        {
            return value.Replace("\"", "\"\"");
        }

        private static void BeginGroup(StreamWriter stream)
        {
            stream.Write(Open);
        }

        private static void EndGroup(StreamWriter stream)
        {
            stream.Write(Close);
        }

        private static void Space(StreamWriter stream)
        {
            stream.Write(" ");
        }
    }
}
