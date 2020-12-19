using System;
using System.Collections.Generic;

namespace SEE.Utils
{
    public partial class ConfigReader
    {
        /// <summary>
        /// Parses the input configuration. For the grammar specification, see <see cref="ConfigReader"/>.
        /// </summary>
        private class Parser
        {
            /// <summary>
            /// Scanner used to transform the input text into tokens.
            /// </summary>
            private readonly Scanner scanner;

            public Dictionary<string, object> Parse()
            {
                Dictionary<string, object> attributes = new Dictionary<string, object>();
                scanner.NextToken();
                if (scanner.CurrentToken() == TokenType.EndToken)
                {
                    // No attribute                
                }
                else
                {
                    ParseAttributeSeq(attributes);
                    ExpectToken(TokenType.EndToken);
                }
                return attributes;
            }

            /// <summary>
            /// Config ::= AttributeSeq EndToken .
            /// </summary>
            /// <param name="input"></param>
            public Parser(string input)
            {
                scanner = new Scanner(input);
            }

            /// <summary>
            /// AttributeSeq ::= { Attribute } .
            /// </summary>
            private void ParseAttributeSeq(Dictionary<string, object> attributes)
            {
                while (scanner.CurrentToken() == TokenType.Label)
                {
                    ParseAttribute(attributes);
                }
            }

            /// <summary>
            /// Attribute ::= Label ':' Value ';' .
            /// </summary>
            private void ParseAttribute(Dictionary<string, object> attributes)
            {
                string label = ParseLabel();
                ExpectToken(TokenType.LabelSeparator);
                object value = ParseValue();
                ExpectToken(TokenType.AttributeSeparator);
                attributes[label] = value;
            }

            /// <summary>
            /// Parses and returns an attribute label.
            /// </summary>
            private string ParseLabel()
            {
                string result = scanner.TokenValue();
                ExpectToken(TokenType.Label);
                return result;
            }

            /// <summary>
            /// Value ::= Bool | Integer | Float | String | Composite .
            //  Bool ::= True | False .
            /// </summary>
            private object ParseValue()
            {
                switch (scanner.CurrentToken())
                {
                    case TokenType.True:
                        scanner.NextToken();
                        return true;
                    case TokenType.False:
                        scanner.NextToken();
                        return false;
                    case TokenType.Float:
                        {
                            TryParseFloat(scanner.TokenValue(), out float result);
                            scanner.NextToken();
                            return result;
                        }
                    case TokenType.Integer:
                        {
                            Int64.TryParse(scanner.TokenValue(), out long result);
                            scanner.NextToken();
                            return result;
                        }
                    case TokenType.String:
                        {
                            string result = scanner.TokenValue();
                            scanner.NextToken();
                            return result;
                        }
                    case TokenType.Open:
                        return ParseComposite();
                    default:
                        throw new Exception($"true, false, integer, float or {{ expected. Current token is {scanner.CurrentToken()}.\n");
                }
            }

            /// <summary>
            /// Composite ::= '{' AttributeSeq '}' .
            /// </summary>
            private Dictionary<string, object> ParseComposite()
            {
                Dictionary<string, object> result = new Dictionary<string, object>();
                ExpectToken(TokenType.Open);
                ParseAttributeSeq(result);
                ExpectToken(TokenType.Close);
                return result;
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
    }
}