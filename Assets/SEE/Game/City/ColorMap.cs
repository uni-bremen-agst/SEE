using System;
using System.Collections;
using System.Collections.Generic;
using SEE.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// Specifies which color is used to render a named property.
    /// </summary>
    [Serializable]
    public class ColorMap : ConfigIO.PersistentConfigItem, IEnumerable<KeyValuePair<string, ColorRange>>
    {
        /// <summary>
        /// Mapping of property name onto color.
        /// </summary>
        [SerializeField]
        [DictionaryDrawerSettings(KeyLabel = "Name", ValueLabel = "Color")]
        private ColorRangeMapping map = new ColorRangeMapping();

        /// <summary>
        /// Operator [].
        /// </summary>
        /// <param name="name">name of the property for which to retrieve the color</param>
        /// <returns>retrieved color for <paramref name="name"/></returns>
        public ColorRange this[string name]
        {
            get { return map[name]; }
            set { map[name] = value; }
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="name"/> is contained in this <see cref="ColorMap"/>
        /// otherwise <c>false</c>. If <c>true</c> is returned, <paramref name="color"/> will have
        /// the color <paramref name="name"/> is mapped onto. Otherwise <paramref name="color"/>
        /// is undefined.
        /// </summary>
        /// <param name="name">the property's name whose color is requested</param>
        /// <param name="color">the color <paramref name="name"/> is mapped onto; defined only
        /// if <c>true</c> is returned</param>
        /// <returns><c>true</c> if <paramref name="name"/> is contained</returns>
        public bool TryGetValue(string name, out ColorRange color)
        {
            return map.TryGetValue(name, out color);
        }

        /// <summary>
        /// The number of elements in the map.
        /// </summary>
        public int Count
        {
            get => map.Count;
        }

        /// <summary>
        /// Resets this <see cref="CodeMap"/> to an empty mapping.
        /// </summary>
        public void Clear()
        {
            map.Clear();
        }

        /// <summary>
        /// Enumerator for all entries of the map.
        /// </summary>
        /// <returns>enumerator</returns>
        public IEnumerator<KeyValuePair<string, ColorRange>> GetEnumerator()
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
        /// Saves this <see cref="ColorMap"/> as a list of groups (metric name, color)
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
                writer.Save(item.Key, NameLabel);
                item.Value.Save(writer, ColorLabel);
                writer.EndGroup();
            }
            writer.EndList();
        }

        /// <summary>
        /// Restores the values of this <see cref="ColorMap"/> from <paramref name="attributes"/>.
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
                    // Each item in the list is a dictionary holding the pair of (name, color).
                    Dictionary<string, object> dict = item as Dictionary<string, object>;
                    // name
                    string name = null;
                    if (!ConfigIO.Restore(dict, NameLabel, ref name))
                    {
                        Debug.LogError($"Entry of {typeof(ColorMap)} has no value for {NameLabel}\n");
                        continue;
                    }
                    // color
                    ColorRange color = new ColorRange();
                    if (!color.Restore(dict, ColorLabel))
                    {
                        Debug.LogError($"Entry of {typeof(ColorMap)} has no value for {ColorLabel}\n");
                        continue;
                    }
                    map[name] = color;
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
        /// The label of the name of the property in the configuration file.
        /// </summary>
        private const string NameLabel = "name";

        /// <summary>
        /// The label of the color of the property in the configuration file.
        /// </summary>
        private const string ColorLabel = "color";
    }
}
