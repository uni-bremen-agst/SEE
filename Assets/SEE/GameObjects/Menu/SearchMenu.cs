using System.Collections.Generic;
using System.Linq;
using SEE.Controls;
using SEE.Game;
using SEE.Game.UI.PropertyDialog;
using UnityEngine;

namespace SEE.GO.Menu
{
    //TODO: Add missing documentation for this class.
    public class SearchMenu: MonoBehaviour
    {
        private PropertyDialog searchDialog;
        private StringProperty searchString;

        private readonly IDictionary<string, ICollection<GameObject>> cachedNodes = new Dictionary<string, ICollection<GameObject>>();

        private void ExecuteSearch()
        {
            IEnumerable<GameObject> results = cachedNodes.Where(x => FilterString(x.Key).Equals(FilterString(searchString.Value)))
                                                         .SelectMany(x => x.Value);
            foreach (GameObject result in results)
            {
                //TODO: Display arrow above game object (reuse the "spears" from evolution?)
                Debug.Log($"Found {result.name}!");
            }

            SEEInput.KeyboardShortcutsEnabled = true;
        }

        private static string FilterString(string input)
        {
            const string zeroWidthSpace = "\u200B";
            return input.ToLower().Trim().Replace(zeroWidthSpace, string.Empty);
        }

        private void Start()
        {
            // Save all nodes in the scene to quickly search them later on
            foreach (GameObject node in SceneQueries.AllGameNodesInScene(true, true))
            {
                string sourceName = node.GetNode().SourceName;
                if (!cachedNodes.ContainsKey(sourceName))
                {
                    cachedNodes[sourceName] = new List<GameObject>();
                }
                cachedNodes[sourceName].Add(node);
            }
                
            searchString = gameObject.AddComponent<StringProperty>();
            searchString.name = "Source name";
            searchString.Description = "The name of the source code component to search for.";
            
            PropertyGroup group = gameObject.AddComponent<PropertyGroup>();
            group.Name = "Search parameters";
            group.AddProperty(searchString);
            
            searchDialog = gameObject.AddComponent<PropertyDialog>();
            searchDialog.Title = "Search for a node";
            searchDialog.Description = "Enter the node name you wish to search for.";
            searchDialog.AddGroup(group);
            
            // Re-enable keyboard shortcuts on cancel
            searchDialog.OnCancel.AddListener(() => SEEInput.KeyboardShortcutsEnabled = true);
            searchDialog.OnConfirm.AddListener(ExecuteSearch);
            
            //TODO: Bool property (regex yes/no)
            //TODO: Bool properties (leaves, nodes)
            //TODO: Selection property (select city)
        }
            
        private void Update()
        {
            if (SEEInput.ToggleSearch())
            {
                searchDialog.DialogShouldBeShown = true;
                SEEInput.KeyboardShortcutsEnabled = false;
            }
        }
    }
}