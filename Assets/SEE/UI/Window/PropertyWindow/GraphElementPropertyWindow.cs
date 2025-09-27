using MoreLinq;
using SEE.DataModel.DG;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SEE.UI.Window.PropertyWindow
{
    /// <summary>
    /// Represents a movable, scrollable window containing properties of a <see cref="GraphElement"/>.
    /// It consists of a search field and a list of properties, where each property is represented by a row
    /// holding the attribute name and its value.
    /// </summary>
    public class GraphElementPropertyWindow : PropertyWindow
    {
        /// <summary>
        /// GraphElement whose properties are to be shown.
        /// </summary>
        public GraphElement GraphElement;

        /// <summary>
        /// Creates the items (rows) for the attributes.
        /// It populates the window.
        /// </summary>
        protected override void CreateItems()
        {
            if (contextMenu.Filter.IncludeHeader)
            {
                Dictionary<string, string> header = new()
                {
                    { "Kind", GraphElement is Node ? "Node" : "Edge" },
                };
                if (GraphElement is Edge edge)
                {
                    header.Add("ID", edge.ID);
                    header.Add("Source", edge.Source.ID);
                    header.Add("Target", edge.Target.ID);
                }
                header.Add("Type", GraphElement.Type);

                // Data Attributes
                Dictionary<string, (string, GameObject gameObject)> headerItems = DisplayAttributes(header);
                groupHolder.Add("Header", headerItems.Values.Select(x => x.gameObject).ToList());
                expandedItems.Add("Header");
            }
            // There are two ways to group the attributes: by value type or by name type.
            // The first one creates groups like "String Attributes", "Int Attributes", etc.
            // according to the kind of graph element attribute kind.
            // The second one creates groups according to the qualified name of the graph element attribute,
            // for example "Source", "Metric", etc. The name is split at the first dot.
            if (!contextMenu.Grouper)
            {
                GroupByValueType();
            }
            else
            {
                GroupByNameType();
            }

            // Sorts the properties
            Sort();

            // Applies the search
            ApplySearch();

            return;

            // Creates the items for the value type group, when attributes should be
            // grouped by their value type (i.e., toggle, string, int, float attributes).
            void GroupByValueType()
            {
                // Toggle Attributes
                if (GraphElement.ToggleAttributes.Count > 0 && contextMenu.Filter.IncludeToggleAttributes)
                {
                    DisplayGroup(PropertyTypes.ToggleAttributes, GraphElement.ToggleAttributes.ToDictionary(item => item, item => true));
                }

                // String Attributes
                if (GraphElement.StringAttributes.Count > 0 && contextMenu.Filter.IncludeStringAttributes)
                {
                    DisplayGroup(PropertyTypes.StringAttributes, GraphElement.StringAttributes);
                }

                // Int Attributes
                if (GraphElement.IntAttributes.Count > 0 && contextMenu.Filter.IncludeIntAttributes)
                {
                    DisplayGroup(PropertyTypes.IntAttributes, GraphElement.IntAttributes);
                }

                // Float Attributes
                if (GraphElement.FloatAttributes.Count > 0 && contextMenu.Filter.IncludeFloatAttributes)
                {
                    DisplayGroup(PropertyTypes.FloatAttributes, GraphElement.FloatAttributes);
                }
            }

            // Creates the items for the name type group, when attributes should be
            // grouped by their name type (i.e., Source, Metric, etc.).
            void GroupByNameType()
            {
                if (GraphElement.ToggleAttributes.Count > 0 && contextMenu.Filter.IncludeToggleAttributes)
                {
                    Dictionary<string, bool> toggleDict = GraphElement.ToggleAttributes.ToDictionary(item => item, item => true);
                    if (groupHolder.ContainsKey("Header"))
                    {
                        DisplayAttributes(toggleDict, group: "Header");
                    }
                    else
                    {
                        Dictionary<string, (string, GameObject gameObject)> toggleItems = DisplayAttributes(toggleDict);
                        groupHolder.Add("Header", toggleItems.Values.Select(x => x.gameObject).ToList());
                        expandedItems.Add("Header");
                    }
                }
                Dictionary<string, object> attributes = new();
                if (GraphElement.StringAttributes.Count > 0 & contextMenu.Filter.IncludeStringAttributes)
                {
                    foreach (KeyValuePair<string, string> pair in GraphElement.StringAttributes)
                    {
                        attributes.Add(InsertDotInFirstPascalCase(pair.Key), pair.Value);
                    }
                }
                if (GraphElement.IntAttributes.Count > 0 & contextMenu.Filter.IncludeIntAttributes)
                {
                    foreach (KeyValuePair<string, int> pair in GraphElement.IntAttributes)
                    {
                        string key = pair.Key;
                        // Block for old gxl files.
                        if (key.Contains("SelectionRange") && !key.Contains("Source"))
                        {
                            key = "Source." + key;
                        }
                        key = InsertDotInFirstPascalCase(pair.Key);
                        /// To remove duplicates it is needed to remove the old one. <see cref="GraphWriter.AppendAttributes"/>
                        if (attributes.ContainsKey(key) && key.Contains("Source.Range"))
                        {
                            attributes.Remove(key);
                        }
                        attributes.Add(key, pair.Value);
                    }
                }
                if (GraphElement.FloatAttributes.Count > 0 & contextMenu.Filter.IncludeFloatAttributes)
                {
                    foreach (KeyValuePair<string, float> pair in GraphElement.FloatAttributes)
                    {
                        attributes.Add(InsertDotInFirstPascalCase(pair.Key), pair.Value);
                    }
                }
                SplitInAttributeGroup(attributes);

                return;

                string InsertDotInFirstPascalCase(string input)
                {
                    // Regular Expression Pattern
                    string pattern = @"^([A-Z][a-z]+)([A-Z][a-z]+)(_.*)$";

                    Regex regex = new(pattern);
                    Match match = regex.Match(input);

                    if (match.Success)
                    {
                        // Build the new string by inserting a period between the matched groups
                        return $"{match.Groups[1].Value}.{match.Groups[2].Value}{match.Groups[3].Value}";
                    }
                    else
                    {
                        // Return the original input if it doesn't match the pattern
                        return input;
                    }
                }
            }
        }
    }
}
