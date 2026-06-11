using System.Linq;
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
        public void Update()
        {
            if (SEEInput.OpenSnapshotsView())
            {
                WindowSpace manager = WindowSpaceManager.ManagerInstance[WindowSpaceManager.LocalPlayer];

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
