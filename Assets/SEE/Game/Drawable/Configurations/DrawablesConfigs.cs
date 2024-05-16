using SEE.Utils.Config;
using System;
using System.Collections.Generic;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// This class can persist the configurations for a list of drawables.
    /// </summary>
    [Serializable]
    public class DrawablesConfigs
    {
        /// <summary>
        /// A list of drawables that should be saved or loaded.
        /// </summary>
        public List<DrawableConfig> Drawables = new();

        /// <summary>
        /// Label in configuration file for the drawables configurations.
        /// </summary>
        private const string DrawablesLabel = "DrawablesConfigs";

        /// <summary>
        /// Saves this instance's attributes using the given <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes.</param>
        internal void Save(ConfigWriter writer)
        {
            writer.BeginList(DrawablesLabel);
            foreach (DrawableConfig drawableConfig in Drawables)
            {
                writer.BeginGroup();
                drawableConfig.Save(writer);
                writer.EndGroup();
            }
            writer.EndList();
        }

        /// <summary>
        /// Loads the given attributes into this instance of the class <see cref="DrawablesConfigs"/>.
        /// </summary>
        /// <param name="attributes">The attributes to load in the format created by
        /// <see cref="ConfigWriter"/>.</param>
        /// <returns>Whether or not the attributes were loaded without any errors.</returns>
        internal bool Restore(Dictionary<string, object> attributes)
        {
            bool errorFree = true;
            if (attributes.TryGetValue(DrawablesLabel, out object drawableList))
            {
                foreach (object item in (List<object>)drawableList)
                {
                    Dictionary<string, object> dict = (Dictionary<string, object>)item;
                    DrawableConfig config = new DrawableConfig();
                    errorFree = config.Restore(dict);
                    Drawables.Add(config);
                }
            }
            else
            {
                errorFree = false;
            }
            return errorFree;
        }
    }
}