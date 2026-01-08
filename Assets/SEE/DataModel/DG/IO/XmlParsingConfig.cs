namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Configuration for parsing XML-based reports using XPath mappings.
    /// </summary>
    public abstract class XmlParsingConfig : ParsingConfig
    {
        /// <summary>
        /// Describes which XML nodes to visit and how to interpret them.
        /// Must not be null when an XML parser uses this configuration.
        /// </summary>
        public XPathMapping XPathMapping = new ();

        /// <summary>
        /// Creates an <see cref="XmlReportParser"/> configured for XML input.
        /// </summary>
        /// <remarks>Preconditions: <see cref="XPathMapping"/> and <see cref="ParsingConfig.ToolId"/> must be initialized.</remarks>
        /// <returns>An <see cref="IReportParser"/> instance for XML reports.</returns>
        internal override IReportParser CreateParser()
        {
            return new XmlReportParser(this);
        }
    }
}
