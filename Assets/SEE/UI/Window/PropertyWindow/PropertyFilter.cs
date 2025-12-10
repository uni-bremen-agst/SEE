namespace SEE.UI.Window.PropertyWindow
{
    /// <summary>
    /// A configurable filter for properties.
    /// </summary>
    public class PropertyFilter
    {
        /// <summary>
        /// Whether to include the data header.
        /// </summary>
        public bool IncludeHeader = true;

        /// <summary>
        /// Whether to include the toggle attributes.
        /// </summary>
        public bool IncludeToggleAttributes = true;

        /// <summary>
        /// Whether to include the string attributes.
        /// </summary>
        public bool IncludeStringAttributes = true;

        /// <summary>
        /// Whether to include the int attributes.
        /// </summary>
        public bool IncludeIntAttributes = true;

        /// <summary>
        /// Whether to include the float attributes.
        /// </summary>
        public bool IncludeFloatAttributes = true;

        /// <summary>
        /// Resets the filter.
        /// </summary>
        public void Reset()
        {
            IncludeHeader = true;
            IncludeToggleAttributes = true;
            IncludeStringAttributes = true;
            IncludeIntAttributes = true;
            IncludeFloatAttributes = true;
        }
    }
}
