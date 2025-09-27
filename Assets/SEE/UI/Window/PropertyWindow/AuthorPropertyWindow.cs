using SEE.GameObjects;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace SEE.UI.Window.PropertyWindow
{
    /// <summary>
    /// A property window showing the properties of an author.
    /// </summary>
    internal class AuthorPropertyWindow : PropertyWindow
    {
        /// <summary>
        /// The author whose properties are shown in this window.
        /// </summary>
        public AuthorSphere author;

        /// <inheritdoc/>
        protected override void CreateItems()
        {
            if (contextMenu.Filter.IncludeHeader)
            {
                Dictionary<string, string> header = new()
                {
                    { "Name", author.Author.Name },
                };
                // Data Attributes
                Dictionary<string, (string, GameObject gameObject)> headerItems = DisplayAttributes(header);
                groupHolder.Add("Header", headerItems.Values.Select(x => x.gameObject).ToList());
                expandedItems.Add("Header");
            }
            /// There are two ways to group the attributes: by value type or by name type.
            /// The first one creates groups like <see cref="PropertyTypes.ToggleAttributes"/>,
            /// <see cref="PropertyTypes.StringAttributes"/>, etc. according to the kind of
            /// graph element attribute kind.
            /// The second one creates groups according to the qualified name of the graph element attribute,
            /// for example "Source", "Metric", etc. The name is split at the first dot.
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
            // grouped by their value type (i.e., boolean, string, int, float attributes).
            void GroupByValueType()
            {
                // Toggle Attributes
                if (contextMenu.Filter.IncludeToggleAttributes)
                {
                    DisplayGroup(PropertyTypes.ToggleAttributes, new Dictionary<string, string>{ { "Is Admin", "yes" } });
                }

                // String Attributes
                if (contextMenu.Filter.IncludeStringAttributes)
                {
                    DisplayGroup(PropertyTypes.StringAttributes, new Dictionary<string, string> { { "E-Mail", author.Author.Email } });
                }

                // Int Attributes
                if (contextMenu.Filter.IncludeIntAttributes)
                {
                    DisplayGroup(PropertyTypes.IntAttributes, new Dictionary<string, string> { { "Number of Commits", "10" } });
                }

                // Float Attributes
                if (contextMenu.Filter.IncludeFloatAttributes)
                {
                    DisplayGroup(PropertyTypes.FloatAttributes, new Dictionary<string, string> { { "Hours", "10.0" } });
                }
            }

            void GroupByNameType()
            {
                Dictionary<string, object> attributes = new();
                if (contextMenu.Filter.IncludeToggleAttributes)
                {
                    attributes.Add("Is Admin", "yes");
                }
                if (contextMenu.Filter.IncludeStringAttributes)
                {
                    attributes.Add("E-Mail", author.Author.Email);
                }
                if (contextMenu.Filter.IncludeIntAttributes)
                {
                    attributes.Add("Number of Commits", "10");
                }
                if (contextMenu.Filter.IncludeFloatAttributes)
                {
                    attributes.Add("Hours", "10.0");
                }
                SplitInAttributeGroup(attributes);
            }
        }
    }
}
