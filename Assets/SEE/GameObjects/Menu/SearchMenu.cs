using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SEE.Controls;
using SEE.Game;
using SEE.Game.UI.Notification;
using SEE.Game.UI.PropertyDialog;
using SEE.Utils;
using UnityEngine;

namespace SEE.GO.Menu
{
    //TODO: Add missing documentation for this class.
    public class SearchMenu: MonoBehaviour
    {
        private PropertyDialog searchDialog;
        private StringProperty searchString;

        private const int BLINK_SECONDS = 10;

        private readonly IDictionary<string, ICollection<GameObject>> cachedNodes = new Dictionary<string, ICollection<GameObject>>();

        private void ExecuteSearch()
        {
            IEnumerable<GameObject> results = cachedNodes.Where(x => FilterString(x.Key).Equals(FilterString(searchString.Value)))
                                                         .SelectMany(x => x.Value);
            int found = 0;
            foreach (GameObject result in results)
            {
                found++;
                if (result.TryGetComponentOrLog(out Renderer cityRenderer))
                {
                    Material material = cityRenderer.sharedMaterials.Last();
                    StartCoroutine(BlinkFor(BLINK_SECONDS, material));
                }
                //TODO: Blink
            }

            if (found == 0)
            {
                ShowNotification.Warn("No nodes found", "No nodes found for the search term "
                                                        + $"'{FilterString(searchString.Value)}'.");
            }
            else if (found == 1)
            {
                ShowNotification.Info($"{found} nodes found", $"Found {found} nodes for search term " 
                                                              + $"'{searchString.Value}'. Nodes will blink for {BLINK_SECONDS} seconds.");
            } 
            else 
            {
                //TODO: Fuzzy search
            }

            SEEInput.KeyboardShortcutsEnabled = true;
        }

        private static IEnumerator BlinkFor(int seconds, Material material)
        {
            Color originalColor = material.color;
            for (int i = seconds*2; i > 0; i--)
            {
                material.color = material.color.Invert();
                yield return new WaitForSeconds(0.5f);
            }

            material.color = originalColor;
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
            searchString.Name = "Source name";
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