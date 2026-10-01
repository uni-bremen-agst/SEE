using System.Collections.Generic;

namespace SEE.Utils.Config
{
    /// <summary>
    /// Defines the interface for a configuration item that can be persisted and restored.
    /// </summary>
    public interface IPersistentConfigItem
    {
        /// <summary>
        /// Saves the configuration attributes by way of the given <paramref name="writer"/>
        /// with the given <paramref name="label"/>.
        /// </summary>
        /// <param name="writer">The configuration writer to be used for the output.</param>
        /// <param name="label">The label to be emitted in front of the configuration attributes.</param>
        void Save(ConfigWriter writer, string label = "");

        /// <summary>
        /// Restores the attributes of this instance from <paramref name="attributes"/> as follows:
        /// If label is neither empty nor null, <paramref name="attributes"/>[<paramref name="label"/>]
        /// is looked up. If it does not exist, nothing else happens and false is returned. If it
        /// exists, the data available in <paramref name="attributes"/>[<paramref name="label"/>] will
        /// be used to restore the attributes of this instance. If at least one such attribute was
        /// restored, true is returned; otherwise false is returned.
        /// If the label is empty or null, <paramref name="attributes"/> direclty is assumed to hold the
        /// data to restore the attributes of this instance.
        /// </summary>
        /// <param name="attributes">If <paramref name="label"/> is null or empty,  holds the data
        /// for restoring the attributes; otherwise <paramref name="attributes"/>[<paramref name="label"/>]
        /// is assumed to hold the necessary data.</param>
        /// <param name="label">The label for the lookup of the data to restore the attributes,
        /// or null or empty.</param>
        /// <returns>True if at least one attribute was successfully restored.</returns>
        bool Restore(Dictionary<string, object> attributes, string label = "");
    }
}
