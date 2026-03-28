using System;
using UnityEngine;

namespace SEE.DataModel.DG.IO
{
    /// <summary>
    /// Configuration for parsing JSON-based reports using JSONPath mappings.
    /// </summary>
    [Serializable]
    public abstract class JsonParsingConfig : ParsingConfig
    {
        /// <summary>
        /// Describes which JSON tokens to visit and how to interpret them.
        /// </summary>
        /// <remarks>This is not a user setting. It will not be saved to a configuration file.
        /// It depends solely on the type of report data and will be set by the subclasses
        /// appropriately.</remarks>
        [HideInInspector]
        public JsonPathMapping JsonMapping = new();

        internal override IReportParser CreateParser()
        {
            return new JsonReportParser(this);
        }
    }
}
