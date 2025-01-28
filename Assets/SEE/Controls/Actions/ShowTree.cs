using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SEE.Game;
using SEE.Game.City;
using SEE.GO;
using SEE.UI.Window;
using SEE.UI.Window.TreeWindow;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Shows or hides the tree view over a code city.
    /// </summary>
    /// <remarks>This component is meant to be attached to a player.</remarks>
    public class ShowTree : MonoBehaviour
    {
        /// <summary>
        /// The local player's window space.
        /// </summary>
        private WindowSpace space;

        /// <summary>
        /// A dictionary mapping the name of a code city to its tree window.
        /// </summary>
        private readonly IDictionary<string, TreeWindow> treeWindows = new Dictionary<string, TreeWindow>();

        /// <summary>
        /// Displays the tree view window for each code city.
        /// </summary>
        private void ShowTreeView()
        {
            GameObject[] cities = GameObject.FindGameObjectsWithTag(Tags.CodeCity);
            if (cities.Length == 0)
            {
                Debug.LogWarning("No code city found. Tree view will be empty.");
                return;
            }
            // We will create a tree view for each code city.
            foreach (GameObject cityObject in cities)
            {
                if (cityObject.TryGetComponent(out AbstractSEECity city))
                {
                    if (city.LoadedGraph == null || treeWindows.ContainsKey(city.name))
                    {
                        continue;
                    }
                    if (!cityObject.TryGetComponent(out TreeWindow window))
                    {
                        window = cityObject.AddComponent<TreeWindow>();
                        window.Graph = city.LoadedGraph;
                    }
                    treeWindows.Add(city.name, window);
                }
            }

            SetupManager().Forget();
            return;

            // Adds the generated tree windows to the local player's window space.
            async UniTaskVoid SetupManager()
            {
                // We need to wait until the WindowSpaceManager has been initialized.
                await UniTask.WaitUntil(() => WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer] != null);
                space = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];
                foreach (string city in treeWindows.Keys)
                {
                    if (!space.Windows.Contains(treeWindows[city]))
                    {
                        space.AddWindow(treeWindows[city]);
                    }
                    space.ActiveWindow = treeWindows[city];
                }
            }
        }

        /// <summary>
        /// Close all tree view windows.
        /// It is assumed that this method is called from a toggle action.
        /// </summary>
        private void HideTreeView()
        {
            // If none of the windows were actually closed, we should instead open them.
            bool anyClosed = false;
            foreach (string city in treeWindows.Keys)
            {
                anyClosed |= space.CloseWindow(treeWindows[city]);
            }
            treeWindows.Clear();
            if (!anyClosed)
            {
                ShowTreeView();
            }
        }

        private void Update()
        {
            if (SEEInput.ToggleTreeView())
            {
                if (treeWindows.Count == 0)
                {
                    ShowTreeView();
                }
                else
                {
                    HideTreeView();
                }
            }
        }
    }
}
