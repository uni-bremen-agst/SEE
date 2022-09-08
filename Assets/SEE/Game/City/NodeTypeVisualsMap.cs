using System;
using System.Collections;
using System.Collections.Generic;
using SEE.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// A mapping of node types onto <see cref="VisualNodeAttributes"/>.
    /// </summary>
    [Serializable]
    public class NodeTypeVisualsMap : ConfigIO.PersistentConfigItem, IEnumerable<KeyValuePair<string, VisualNodeAttributes>>
    {
        /// <summary>
        /// Mapping of node type name onto <see cref="VisualNodeAttributes"/>.
        /// </summary>
        [SerializeField]
        [DictionaryDrawerSettings(KeyLabel = "Node Type", ValueLabel = "Visual Attributes")]
        private VisualNodeAttributesMapping map = new VisualNodeAttributesMapping();

        /// <summary>
        /// Operator [].
        /// </summary>
        /// <param name="nodeType">name of the node type for which to retrieve the <see cref="VisualNodeAttributes"/></param>
        /// <returns>retrieved <see cref="VisualNodeAttributes"/> for <paramref name="nodeType"/></returns>
        public VisualNodeAttributes this[string nodeType]
        {
            get { return map[nodeType]; }
            set { map[nodeType] = value; }
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="nodeType"/> is contained in this <see cref="NodeTypeVisualsMap"/>
        /// otherwise <c>false</c>. If <c>true</c> is returned, <paramref name="attributes"/> will have
        /// the <see cref="VisualNodeAttributes"/> <paramref name="nodeType"/> is mapped onto.
        /// Otherwise <paramref name="attributes"/> is undefined.
        /// </summary>
        /// <param name="nodeType">the node type's name whose <see cref="VisualNodeAttributes"/> are requested</param>
        /// <param name="attributes">the <see cref="VisualNodeAttributes"/> <paramref name="nodeType"/> is mapped onto;
        /// defined only if <c>true</c> is returned</param>
        /// <returns><c>true</c> if <paramref name="nodeType"/> is contained</returns>
        public bool TryGetValue(string nodeType, out VisualNodeAttributes attributes)
        {
            return map.TryGetValue(nodeType, out attributes);
        }

        /// <summary>
        /// The number of elements in the map.
        /// </summary>
        public int Count => map.Count;

        /// <summary>
        /// Returns all <see cref="VisualNodeAttributes"/> stored in this map.
        /// </summary>
        public ICollection<VisualNodeAttributes> Values => map.Values;

        /// <summary>
        /// Resets this <see cref="NodeTypeVisualsMap"/> to an empty mapping.
        /// </summary>
        public void Clear()
        {
            map.Clear();
        }

        /// <summary>
        /// Enumerator for all entries of the map.
        /// </summary>
        /// <returns>enumerator</returns>
        public IEnumerator<KeyValuePair<string, VisualNodeAttributes>> GetEnumerator()
        {
            return map.GetEnumerator();
        }

        /// <summary>
        /// Enumerator for all entries of the map.
        /// </summary>
        /// <returns>enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Saves this <see cref="NodeTypeVisualsMap"/> as a list of groups (node-type name, <see cref="NodeTypeVisualsMap"/>)
        /// using <paramref name="writer"/> under the given <paramref name="label"/>.
        /// Each pair is saved as a list
        /// </summary>
        /// <param name="writer">to be used for writing the settings</param>
        /// <param name="label">the outer label grouping the settings</param>
        public void Save(ConfigWriter writer, string label)
        {
            writer.BeginList(label);
            foreach (var item in map)
            {
                writer.BeginGroup();
                writer.Save(item.Key, NodeTypeLabel);
                item.Value.Save(writer, VisualNodeAttributesLabel);
                writer.EndGroup();
            }
            writer.EndList();
        }

        /// <summary>
        /// Restores the values of this <see cref="NodeTypeVisualsMap"/> from <paramref name="attributes"/>.
        /// </summary>
        /// <param name="attributes">dictionary of attributes from which to retrieve the settings</param>
        /// <param name="label">the label for the settings (a key in <paramref name="attributes"/>)</param>
        /// <returns>true if at least one attribute was successfully restored</returns>
        public bool Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object list))
            {
                bool result = false;
                foreach (object item in list as List<object>)
                {
                    // Each item in the list is a dictionary holding the pair of (node-type name, VisualNodeAttributes).
                    Dictionary<string, object> dict = item as Dictionary<string, object>;
                    // node-type name
                    string name = null;
                    if (!ConfigIO.Restore(dict, NodeTypeLabel, ref name))
                    {
                        Debug.LogError($"Entry of {typeof(NodeTypeVisualsMap)} has no value for {NodeTypeLabel}\n");
                        continue;
                    }
                    // VisualNodeAttributes
                    VisualNodeAttributes visualNodeAttributes = new VisualNodeAttributes();
                    visualNodeAttributes.Restore(dict, VisualNodeAttributesLabel);

                    map[name] = visualNodeAttributes;
                    result = true;
                }
                return result;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// The label of the name of the node type in the configuration file.
        /// </summary>
        private const string NodeTypeLabel = "nodeType";

        /// <summary>
        /// The label of the <see cref="VisualNodeAttributes"/> in the configuration file.
        /// </summary>
        private const string VisualNodeAttributesLabel = "visualNodeAttributes";
    }
}
