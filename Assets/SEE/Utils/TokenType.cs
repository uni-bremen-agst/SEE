namespace SEE.Utils
{
    public partial class ConfigReader
    {
        /// <summary>
        /// The tokens of the configuration file.
        /// </summary>
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
            OpenList,           // '['
            CloseList,          // ']'
            AttributeSeparator, // AttributeSeparator = ';'
            LabelSeparator,     // LabelSeparator = ':'
            EndToken,           // end of input
            Error               // in case of malformed input
        }
    }
}