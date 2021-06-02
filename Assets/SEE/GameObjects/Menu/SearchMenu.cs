using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FuzzySharp;
using SEE.Controls;
using SEE.Game;
using SEE.Game.Evolution;
using SEE.Game.UI.Menu;
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

        private bool stillHighlighting = true;

        private const int BLINK_SECONDS = 15;
        private const float MARKER_HEIGHT = 1f;
        private const float MARKER_WIDTH = 0.01f;

        private static readonly Color MARKER_COLOR = Color.red;

        private readonly IDictionary<string, ICollection<GameObject>> cachedNodes = new Dictionary<string, ICollection<GameObject>>();
        private SimpleMenu resultMenu;
        /// <summary>
        /// A list containing all entries in the <see cref="resultMenu"/>.
        /// </summary>
        /// <remarks>This is not an <see cref="IList{T}"/> because the <c>ForEach</c> function is not defined for the
        /// interface, which is used in <see cref="ShowResultsMenu"/>.</remarks>
        private readonly List<MenuEntry> resultMenuEntries = new List<MenuEntry>();

        private void ExecuteSearch()
        {
            SEEInput.KeyboardShortcutsEnabled = true;
            // Format: (score, name, found game object)
            IEnumerable<(int, string, GameObject)> results =
                Process.ExtractTop(FilterString(searchString.Value), cachedNodes.Keys)
                       .Where(x => x.Score > 0) // results with score 0 are usually garbage
                       .SelectMany(x => cachedNodes[x.Value].Select(y => (x.Score, x.Value, y)))
                       .ToList();
            
            int found = results.Count();
            switch (found)
            {
                case 0:
                    ShowNotification.Warn("No nodes found", "No nodes found for the search term "
                                                            + $"'{FilterString(searchString.Value)}'.");
                    break;
                case 1:
                    HighlightNode(results.First().Item3, results.First().Item2);
                    break;
                default:
                    ShowResultsMenu(results);
                    break;
            }

        }
        
        private void ShowResultsMenu(IEnumerable<(int, string, GameObject)> results)
        {
            if (resultMenu == null)
            {
                // Initialize result menu
                resultMenu = gameObject.AddComponent<SimpleMenu>();
                resultMenu.Title = "Search Results";
                resultMenu.Description = "Please select the node you wish to highlight.";
                resultMenu.Icon = Resources.Load<Sprite>("Materials/ModernUIPack/Search");
            }

            // Entries will be greyed out the further they go
            resultMenuEntries.ForEach(resultMenu.RemoveEntry); // clean up previous entries
            resultMenuEntries.Clear();
            resultMenuEntries.AddRange(results.Select(x => new MenuEntry(() => MenuEntryAction(x.Item3, x.Item2), 
                                                                         x.Item2, entryColor: ScoreColor(x.Item1))));
            resultMenuEntries.ForEach(resultMenu.AddEntry);
            resultMenu.ShowMenu(true);

            // Highlight node and close menu when entry was chosen
            void MenuEntryAction(GameObject chosen, string chosenName)
            {
                HighlightNode(chosen, chosenName);
                resultMenu.ShowMenu(false);
            }

            // Returns a color between black and gray, the higher the given score the grayer it is
            static Color ScoreColor(int score)
            {
                Debug.Log(score);
                return Color.Lerp(Color.gray, Color.white, score / 100f);
            }
        }

        private void HighlightNode(GameObject result, string resultName)
        {
            ShowNotification.Info($"Highlighting '{resultName}'", 
                                  $"The selected node will be blinking and marked by a spear for {BLINK_SECONDS}.");
            GameObject cityObject = SceneQueries.GetCodeCity(result.transform).gameObject;
            if (result.TryGetComponentOrLog(out Renderer cityRenderer) &&
                cityObject.TryGetComponentOrLog(out AbstractSEECity city))
            {
                // Display marker above the node
                GraphRenderer graphRenderer = new GraphRenderer(city, null);
                Marker marker = new Marker(graphRenderer, MARKER_WIDTH, MARKER_HEIGHT, MARKER_COLOR,
                                           default, default, AbstractAnimator.DefaultAnimationTime);
                Material material = cityRenderer.sharedMaterials.Last();
                StartCoroutine(BlinkFor(BLINK_SECONDS, material));

                StartCoroutine(RemoveMarkerWhenDone(marker));
                marker.MarkBorn(result);
            }

            static IEnumerator RemoveMarkerWhenDone(Marker marker)
            {
                yield return new WaitForSeconds(BLINK_SECONDS);
                marker.Clear();
            }
        }

        private IEnumerator BlinkFor(int seconds, Material material)
        {
            Color originalColor = material.color;
            stillHighlighting = true;
            for (int i = seconds*2; i > 0; i--)
            {
                if (!stillHighlighting) 
                {
                    // Another search has been started
                    break;
                }
                material.color = material.color.Invert();
                yield return new WaitForSeconds(0.5f);
            }

            material.color = originalColor;
        }

        private static string FilterString(string input)
        {
            const string zeroWidthSpace = "\u200B";
            return input.Trim().Replace(zeroWidthSpace, string.Empty);
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
            searchDialog.Title = "Search node";
            searchDialog.Description = "Enter the node name you wish to search for.";
            searchDialog.Icon = Resources.Load<Sprite>("Materials/ModernUIPack/Search");
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
                stillHighlighting = true;
                searchDialog.DialogShouldBeShown = true;
                SEEInput.KeyboardShortcutsEnabled = false;
            }
        }
    }
}