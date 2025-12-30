namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Configuration for parsing JSON-based reports using JSONPath mappings.
    /// </summary>
    public abstract class JsonParsingConfig : ParsingConfig
    {
        /// <summary>
        /// Describes which JSON tokens to visit and how to interpret them.
        /// </summary>
        public JsonPathMapping JsonMapping = new ();

        internal override IReportParser CreateParser()
        {
            return new JsonReportParser(this);
        }
    }
}
