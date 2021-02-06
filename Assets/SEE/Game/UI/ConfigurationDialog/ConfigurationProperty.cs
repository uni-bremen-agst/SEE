namespace SEE.Game.UI.ConfigurationDialog
{
    /// <summary>
    /// A property which can be changed in a <see cref="ConfigurationDialog"/>
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    public class ConfigurationProperty<T>
    {
        /// <summary>
        /// The configurable property.
        /// </summary>
        protected T Property;
    }
}