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

        private void Start()
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
                if (cityObject.TryGetComponentOrLog(out AbstractSEECity city))
                {
                    if (city.LoadedGraph == null)
                    {
                        continue;
                    }
                    TreeWindow window = gameObject.AddComponent<TreeWindow>();
                    window.graph = city.LoadedGraph;
                    treeWindows.Add(city.name, window);
                }
            }

            SetupManager().Forget();
            return;

            // Adds the generated tree windows to the local player's window space.
            async UniTaskVoid SetupManager()
            {
                // We need to wait until the WindowSpaceManager has been initialized.
                await UniTask.WaitUntil(() => WindowSpaceManager.ManagerInstance[WindowSpaceManager.LOCAL_PLAYER] != null);
                space = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LOCAL_PLAYER];
                foreach (string city in treeWindows.Keys)
                {
                    space.AddWindow(treeWindows[city]);
                    space.ActiveWindow = treeWindows[city];
                }
            }
        }
    }
}
