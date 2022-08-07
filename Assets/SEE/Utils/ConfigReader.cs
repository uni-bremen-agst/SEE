using System;
using System.Collections.Generic;

namespace SEE.Utils
{
    /// <summary>
    /// Thrown in case of syntax errors.
    /// </summary>
    public class SyntaxError : Exception
    {
        public SyntaxError(string message) : base(message)
        {
            // intentionally left blank
        }
    }

    /// <summary>
    /// Parser for configuration settings according to the following
    /// grammar in EBNF:
    ///
    ///  Config ::= AttributeSeq EndToken .
    ///  AttributeSeq ::= { Attribute } .
    ///  Attribute ::= Label ':' Value ';' .
    ///  Value ::= Bool | Integer | Float | String | Composite | List.
    ///  Bool ::= True | False .
    ///  Composite ::= '{' AttributeSeq '}' .
    ///  List ::= '[' (ValueSeq)? '] .
    ///  ValueSeq ::= { Value ';' } .
    /// </summary>
    public partial class ConfigReader : ConfigIO, IDisposable
    {
        /// <summary>
        /// From where to read the input.
        /// </summary>
        private readonly System.IO.StreamReader stream;

        /// <summary>
        /// Constructor. Does not actually parse the file. You need to run
        /// <see cref="Read"/> later.
        /// </summary>
        /// <param name="filename">name of the file to be parsed</param>
        public ConfigReader(string filename)
        {
            stream = new System.IO.StreamReader(filename);
        }

        /// <summary>
        /// Closes the input stream.
        /// </summary>
        public void Dispose()
        {
            stream.Close();
        }

        /// <summary>
        /// Parses the file whose name was passed to the constructor
        /// and returns the read configuration settings therein.
        ///
        /// Throws an exception if the file content does not conform to the grammar.
        /// </summary>
        /// <returns>the collected attribute values as (nested) dictionary</returns>
        public Dictionary<string, object> Read()
        {
            return Parse(stream.ReadToEnd());
        }

        /// <summary>
        /// Parses <paramref name="input"/> for configuration setttings.
        ///
        /// Throws an exception if <paramref name="input"/> does not conform to the grammar.
        /// </summary>
        /// <param name="input">input to be parsed</param>
        /// <returns>the collected attribute values as (nested) dictionary</returns>
        public static Dictionary<string, object> Parse(string input)
        {
            return new Parser(input).Parse();
        }

        /// <summary>
        /// The number style acceptable when parsing float numbers.
        /// Implied by System.Globalization.NumberStyles.Float are AllowLeadingWhite, AllowTrailingWhite,
        /// AllowLeadingSign, AllowDecimalPoint, AllowExponent. We also allow AllowThousands.
        /// </summary>
        private const System.Globalization.NumberStyles FloatStyle = System.Globalization.NumberStyles.Float
                    | System.Globalization.NumberStyles.AllowThousands;

        /// <summary>
        /// Tries to parse a float from <paramref name="s"/>. Upon success, the float value
        /// is returned in <paramref name="value"/>.
        /// </summary>
        /// <param name="s">a string from which to parse a float (can also be Infinity and -Infinity)</param>
        /// <param name="value">the parsed float value; defined only if <c>true</c> was returned</param>
        /// <returns>true if a float could be parsed</returns>
        private static bool TryParseFloat(string s, out float value)
        {
            return Single.TryParse(s: s, style: FloatStyle,
                                   System.Globalization.CultureInfo.InvariantCulture, out value);
        }
    }
}