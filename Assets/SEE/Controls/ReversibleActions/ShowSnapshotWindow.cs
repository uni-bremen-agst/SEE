using System.Linq;
using SEE.Controls.KeyActions;
using SEE.Controls.Players;
using SEE.UI.Window;
using SEE.UI.Window.SnapshotsWindow;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Provides the ability to detect a user intent by key action (<see cref="KeyActions.KeyAction.OpenSnapshotWindow"/>), to open the snapshot window.
    /// </summary>
    public class ShowSnapshotWindow : MonoBehaviour
    {
        /// <summary>
        /// Event loop, check every frame, if the buttons are pressed.
        /// </summary>
        private void Update()
        {
            if (SEEInput.OpenSnapshotsView())
            {
                WindowSpace manager = WindowSpaceManager.WindowSpaceOfLocalPlayer;

                if (manager.Windows.OfType<SnapshotsWindow>().FirstOrDefault() is { } snapshotWindow)
                {
                    manager.ActiveWindow = snapshotWindow;
                    return;
                }

                SnapshotsWindow window = gameObject.AddComponent<SnapshotsWindow>();
                manager.AddWindow(window);
            }
        }
    }
}
