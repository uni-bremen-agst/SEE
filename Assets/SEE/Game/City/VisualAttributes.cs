using System;
using System.Collections.Generic;
using SEE.Utils;

namespace SEE.Game.City
{

    /// <summary>
    /// Abstract common super class for all settings influencing the visual
    /// appearance of game objects drawn in the scene.
    /// </summary>
    [Serializable]
    public abstract class VisualAttributes
    {
        /// <summary>
        /// Saves the settings in the configuration file.
        /// </summary>
        /// <param name="writer">to be used for writing the settings</param>
        /// <param name="label">the outer label grouping the settings</param>
        public abstract void Save(ConfigWriter writer, string label);
        /// <summary>
        /// Restores the settings from <paramref name="attributes"/> under the key <paramref name="label"/>.
        /// The latter must be the label under which the settings were grouped, i.e., the same
        /// value originally passed in <see cref="Save(ConfigWriter, string)"/>.
        /// </summary>
        /// <param name="attributes">dictionary of attributes from which to retrieve the settings</param>
        /// <param name="label">the label for the settings (a key in <paramref name="attributes"/>)</param>
        public abstract void Restore(Dictionary<string, object> attributes, string label);
    }
}
