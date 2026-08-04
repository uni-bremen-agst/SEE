using System.Linq;
using SEE.UI.Window;
using SEE.UI.Window.CypherQueryWindow;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Provides the ability to detect a user intent by key action (<see cref="KeyActions.KeyAction.OpenCypherQueryWindow"/>), to open the cypher query window.
    /// </summary>
    public class ShowCypherQueryWindow : MonoBehaviour
    {
        /// <summary>
        /// Event loop, check every frame, if the buttons are pressed.
        /// </summary>
        private void Update()
        {
            if (SEEInput.OpenCypherQuerysView())
            {

                WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];

                if (manager.Windows.OfType<CypherQueryWindow>().FirstOrDefault() is { } cypherqueryWindow)
                {
                    manager.ActiveWindow = cypherqueryWindow;
                    return;
                }

                CypherQueryWindow window = gameObject.AddComponent<CypherQueryWindow>();
                manager.AddWindow(window);
            }
        }
    }
}
