using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FuzzySharp;
using SEE.Controls;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.Evolution;
using SEE.Game.UI.Menu;
using SEE.Game.UI.Notification;
using SEE.Game.UI.PropertyDialog;
using SEE.Utils;
using UnityEngine;

namespace SEE.GO.Menu
{
    /// <summary>
    /// A menu which allows its user to fuzzy search for nodes by entering the
    /// source name of a node.
    /// </summary>
    public class SearchMenu: MonoBehaviour
    {
        /// <summary>
        /// The time (in seconds) the found node will blink.
        /// </summary>
        private const int BLINK_SECONDS = 15;

        /// <summary>
        /// The time (in seconds) between color inversions for found nodes.
        /// </summary>
        private const float BLINK_INTERVAL = 0.3f;

        /// <summary>
        /// The height of the marker used to mark the found node.
        /// </summary>
        private const float MARKER_HEIGHT = 1f;

        /// <summary>
        /// The width of the marker used to mark the found node.
        /// </summary>
        private const float MARKER_WIDTH = 0.01f;

        /// <summary>
        /// The dialog in which the search query can be entered.
        /// </summary>
        private PropertyDialog searchDialog;

        /// <summary>
        /// The property which contains the searched query.
        /// </summary>
        private StringProperty searchString;

        /// <summary>
        /// The menu in which the search results are listed.
        /// The user can select the desired node here.
        /// </summary>
        private SimpleMenu resultMenu;

        /// <summary>
        /// Whether we're currently highlighting a node.
        /// Will be set to true when a node starts blinking, and will be set to false when a new search is started,
        /// which will cause the node to stop blinking.
        /// </summary>
        private bool stillHighlighting = true;

        /// <summary>
        /// The color of the marker pointing to the found node.
        /// </summary>
        private static readonly Color MARKER_COLOR = Color.red;

        /// <summary>
        /// A mapping from names to a list of nodes with that name.
        /// Is constructed in the <see cref="Start"/> method in order not to call expensive <c>Find</c> methods every
        /// time a search is executed. Note that this implies that changes to the cities while the game is running
        /// will not be reflected in the search.
        /// </summary>
        private readonly IDictionary<string, ICollection<GameObject>> cachedNodes = new Dictionary<string, ICollection<GameObject>>();

        /// <summary>
        /// A list containing all entries in the <see cref="resultMenu"/>.
        /// </summary>
        /// <remarks>This is not an <see cref="IList{T}"/> because the <c>ForEach</c> function is not defined for the
        /// interface, which is used in <see cref="ShowResultsMenu"/>.</remarks>
        private readonly List<MenuEntry> resultMenuEntries = new List<MenuEntry>();

        /// <summary>
        /// Executes the search with the values entered in the <see cref="searchDialog"/>.
        /// </summary>
        private void ExecuteSearch()
        {
            SEEInput.KeyboardShortcutsEnabled = true;
            // Format: (score, name, found game object)
            IEnumerable<(int, string, GameObject)> results =
                Process.ExtractTop(FilterString(searchString.Value), cachedNodes.Keys)
                       .Where(x => x.Score > 0) // results with score 0 are usually garbage
                       .SelectMany(x => cachedNodes[x.Value].Select(y => (x.Score, x.Value, y)))
                       .ToList();

            // In cases there are duplicates in the result, we append the filename too
            HashSet<string> encounteredNames = new HashSet<string>();
            results = results.GroupBy(x => x.Item2)
                             .SelectMany(x => x.Select((entry, i) => (entry, index: i)))
                             .Select(x =>
                             {
                                 ((int score, string name, GameObject gameObject) entry, int index) = x;
                                 if (index <= 0)
                                 {
                                     return entry;
                                 }
                                 else
                                 {
                                     string newName = $"{entry.name} ({entry.gameObject.GetNode().SourceFile ?? index.ToString()})";
                                     if (!encounteredNames.Contains(newName))
                                     {
                                         encounteredNames.Add(newName);
                                     }
                                     else
                                     {
                                         // If this node exists multiple times within this filename,
                                         // we append the index to it.
                                         newName += $" ({index.ToString()})";
                                     }
                                     return (entry.score, newName, entry.gameObject);
                                 }
                             });


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

        /// <summary>
        /// This will show a menu with search results to the user in case more than one node was found.
        /// </summary>
        /// <param name="results">A list of found nodes represented by 3-tuples in the format
        /// [score (from fuzzy search), name, node game object].</param>
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

            // Entries will be greyed out the further they go.
            resultMenuEntries.ForEach(resultMenu.RemoveEntry); // Clean up previous entries.
            resultMenuEntries.Clear();
            resultMenuEntries.AddRange(results.Select(x => new MenuEntry(() => MenuEntryAction(x.Item3, x.Item2),
                                                                         x.Item2, entryColor: ScoreColor(x.Item1))));
            resultMenuEntries.ForEach(resultMenu.AddEntry);
            resultMenu.ShowMenu(true);

            // Highlight node and close menu when entry was chosen.
            void MenuEntryAction(GameObject chosen, string chosenName)
            {
                HighlightNode(chosen, chosenName);
                resultMenu.ShowMenu(false);
            }

        }

        // Returns a color between black and gray, the higher the given score the grayer it is.
        public static Color ScoreColor(int score) => Color.Lerp(Color.gray, Color.white, score / 100f);

        /// <summary>
        /// Highlights the given <paramref name="result"/>> node with the name <paramref name="resultName"/>
        /// by displaying a marker above it and starting the <see cref="BlinkFor"/> coroutine.
        /// </summary>
        /// <param name="result">The game object of the node which shall be highlighted.</param>
        /// <param name="resultName">The name of the node which shall be highlighted.</param>
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
                Material material = cityRenderer.sharedMaterial;
                BlinkFor(material).Forget();
                RemoveMarkerWhenDone(marker).Forget();
                marker.MarkBorn(result);
            }

            async UniTaskVoid RemoveMarkerWhenDone(Marker marker)
            {
                // Remove marker either when a new search is started or when time is up
                await UniTask.WhenAny(UniTask.Delay(TimeSpan.FromSeconds(BLINK_SECONDS)),
                                      UniTask.WaitUntil(() => !stillHighlighting));
                marker.Clear();
            }
        }

        /// <summary>
        /// Inverts the given <paramref name="material"/>'s color periodically every <see cref="BLINK_INTERVAL"/>
        /// seconds for <see cref="BLINK_SECONDS"/> seconds.
        /// </summary>
        /// <param name="material">The material whose color to invert.</param>
        private async UniTaskVoid BlinkFor(Material material)
        {
            Color originalColor = material.color;
            stillHighlighting = true;
            for (float i = BLINK_SECONDS; i > 0; i -= BLINK_INTERVAL)
            {
                if (!stillHighlighting)
                {
                    // Another search has been started
                    break;
                }
                material.color = material.color.Invert();
                await UniTask.Delay(TimeSpan.FromSeconds(BLINK_INTERVAL));
            }

            material.color = originalColor;
        }

        /// <summary>
        /// Removes the zero-width-space from the given <paramref name="input"/>, as well as whitespace at the
        /// beginning and end.
        /// </summary>
        /// <param name="input">The string which shall be filtered.</param>
        /// <returns>The filtered string.</returns>
        public static string FilterString(string input)
        {
            const string zeroWidthSpace = "\u200B";
            return input.Trim().Replace(zeroWidthSpace, string.Empty);
        }

        /// <summary>
        /// Constructs the <see cref="cachedNodes"/> and the <see cref="searchDialog"/>.
        /// </summary>
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

        /// <summary>
        /// Checks whether the <see cref="searchDialog"/> shall be opened.
        /// </summary>
        private void Update()
        {
            if (SEEInput.ToggleSearch())
            {
                stillHighlighting = false;
                searchDialog.DialogShouldBeShown = true;
                SEEInput.KeyboardShortcutsEnabled = false;
            }
        }
    }
}