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

            /// <summary>
            /// Parses the input (passed to the constructor) and returns all collected attributes
            /// to the resulting dictionary. The key of a dictionary entry is the label retrieved
            /// from the input and the value of the dictionary entry is the value of that label.
            /// In case of a composite attribute, the value is a nested dictionary.
            /// </summary>
            /// <returns>a (nested) dictionary with all attributes collected from the input</returns>
            /// <exception cref="SyntaxError">will be thrown in case of a syntax error</exception>
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
                    case TokenType.OpenList:
                        return ParseList();
                    default:
                        Error($"true, false, integer, float or {{ expected. Current token is {scanner.CurrentToken()}.\n");
                        return null;
                }
            }

            /// <summary>
            /// Composite ::= '{' AttributeSeq '}' .
            /// </summary>
            /// <returns>the collected attributes (labels and values)</returns>
            private Dictionary<string, object> ParseComposite()
            {
                Dictionary<string, object> result = new Dictionary<string, object>();
                ExpectToken(TokenType.Open);
                ParseAttributeSeq(result);
                ExpectToken(TokenType.Close);
                return result;
            }


            /// <summary>
            /// List ::= '[' ValueSeq? '] .
            /// </summary>
            /// <returns>the collected values as a list</returns>
            private List<object> ParseList()
            {
                List<object> result;
                ExpectToken(TokenType.OpenList);
                if (scanner.CurrentToken() != TokenType.CloseList)
                {
                    result = ParseValueSeq();
                }
                else
                {
                    result = new List<object>();
                }
                ExpectToken(TokenType.CloseList);
                return result;
            }

            /// <summary>
            /// ValueSeq ::= { Value ';' } .
            /// </summary>
            /// <returns>the collected values as a list</returns>
            private List<object> ParseValueSeq()
            {
                List<object> result = new List<object>() { ParseValue() };
                ExpectToken(TokenType.AttributeSeparator);
                // the list is ended by ']'
                while (scanner.CurrentToken() != TokenType.CloseList)
                {                    
                    result.Add(ParseValue());
                    ExpectToken(TokenType.AttributeSeparator);
                }
                return result;
            }

            /// <summary>
            /// Checks whether the current token is <paramref name="expected"/>. If not,
            /// an exception will be thrown. Moves to the next token.
            /// </summary>
            /// <param name="expected">the expected token</param>
            private void ExpectToken(TokenType expected)
            {
                if (scanner.CurrentToken() != expected)
                {
                    Error($"Expected token: {expected}. Current token: {scanner.CurrentToken()}");
                }
                scanner.NextToken();
            }

            /// <summary>
            /// Throws <see cref="SyntaxError"/> with given <paramref name="message"/>.
            /// </summary>
            /// <param name="message">message to be added to thrown exception</param>
            /// <exception cref="SyntaxError">will be thrown</exception>
            private void Error(string message)
            {
                throw new SyntaxError($"Line {scanner.CurrentLineNumber()}: '{scanner.CurrentLine()}'. " + message);
            }
        }
    }
}