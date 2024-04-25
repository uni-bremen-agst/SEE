using SEE.Utils;
using SEE.Utils.Config;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// The configuration class for <see cref="Vector3"/>
    /// </summary>
    public class Vector3Config : ConfigIO.IPersistentConfigItem
    {
        /// <summary>
        /// Is the Vector3 of the configuration. 
        /// It will be needed for the list of Vector3 of the line renderer.
        /// </summary>
        public Vector3 vector;

        /// <summary>
        /// Label for the x coordinate of a vector3.
        /// </summary>
        private const string XLabel = "X";

        /// <summary>
        /// Label for the y coordinate of a vector3.
        /// </summary>
        private const string YLabel = "Y";

        /// <summary>
        /// Label for the z coordinate of a vector3.
        /// </summary>
        private const string ZLabel = "Z";

        /// <summary>
        /// Given the representation of a <see cref="Vector3Config"/> as created by the <see cref="ConfigWriter"/>, this
        /// method parses the attributes from that representation and puts them into this <see cref="Vector3Config"/>
        /// instance.
        /// </summary>
        /// <param name="attributes">A list of labels (strings) of attributes and their values (objects). This
        /// has to be the representation of a <see cref="Vector3Config"/> as created by
        /// <see cref="ConfigWriter"/>.</param>
        /// <returns>Whether or not the <see cref="Vector3Config"/> was loaded without errors.</returns>
        public bool Restore(Dictionary<string, object> attributes, string label = "")
        {
            if (attributes.TryGetValue(XLabel, out object x) &&
                attributes.TryGetValue(YLabel, out object y) &&
                attributes.TryGetValue(ZLabel, out object z))
            {
                vector.x = (float)x;
                vector.y = (float)y;
                vector.z = (float)z;
                return true;
            } else
            {
                return false;
            }
        }

        /// <summary>
        /// Writes this instances' attributes into the given <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes into.</param>
        public void Save(ConfigWriter writer, string label = "")
        {
            writer.Save(vector, label);
        }
    }
}