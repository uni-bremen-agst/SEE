using SEE.Utils.Config;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.Drawable.Configurations
{
    /// <summary>
    /// The configuration class for <see cref="Vector3"/>.
    /// </summary>
    public class Vector3Config : ConfigIO.IPersistentConfigItem
    {
        /// <summary>
        /// Is the value of the configuration.
        /// It will be needed for the list of Vector3 of the line renderer.
        /// </summary>
        public Vector3 Value;

        #region Config I/O

        /// <summary>
        /// Label for the x coordinate of a vector3.
        /// </summary>
        private const string xLabel = "X";

        /// <summary>
        /// Label for the y coordinate of a vector3.
        /// </summary>
        private const string yLabel = "Y";

        /// <summary>
        /// Label for the z coordinate of a vector3.
        /// </summary>
        private const string zLabel = "Z";

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
            if (attributes.TryGetValue(xLabel, out object x) &&
                attributes.TryGetValue(yLabel, out object y) &&
                attributes.TryGetValue(zLabel, out object z))
            {
                Value.x = (float)x;
                Value.y = (float)y;
                Value.z = (float)z;
                return true;
            } else
            {
                return false;
            }
        }

        /// <summary>
        /// Saves this instance's attributes using the given <see cref="ConfigWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ConfigWriter"/> to write the attributes.</param>
        public void Save(ConfigWriter writer, string label = "")
        {
            writer.Save(Value, label);
        }

        #endregion
    }
}