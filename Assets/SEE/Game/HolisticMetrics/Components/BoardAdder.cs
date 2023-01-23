using SEE.Game.UI.HolisticMetrics;
using SEE.Game.UI.PropertyDialog.HolisticMetrics;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.HolisticMetrics.Components
{
    /// <summary>
    /// This class can be attached to any GameObject in the scene that you want to add a metrics board onto (probably
    /// the floor). The GameObject needs to have a Collider on it for this script to work. When you attach this
    /// component to a GameObject it will listen for left clicks on the GameObject and once a left click happens, it
    /// will show the user a dialog allowing him to further configure a metrics board. For this component to work
    /// properly, you would also need to set it up using the Setup() method.
    /// </summary>
    internal class BoardAdder : MonoBehaviour
    {
        /// <summary>
        /// Whether or not the addition is done which would mean that all BoardAdders can be deleted. Currently, there
        /// is only one GameObject this BoardAdder is attached to. But should there ever be more than one surface this
        /// BoardAdder will be attached to, it makes sense to make this bool static so that all instances share it.
        /// </summary>
        private static bool positioningDone;

        private static bool gotPosition;

        /// <summary>
        /// The configuration of the board to be created. We should be getting this from the
        /// <see cref="AddBoardDialog"/> and we expect it to only contain the name so far. In this component, the
        /// position will be added to it. Then we will pass it on to the rotation dialog where the rotation will be
        /// added.
        /// </summary>
        private static Vector3 position;

        /// <summary>
        /// This is only used to know the height at which to create the new board, because the prefab has a certain
        /// height so it is exactly over the ground.
        /// </summary>
        private static GameObject boardPrefab;

        /// <summary>
        /// When the GameObject registers a mouse click, we get the position of the hit, create a new BoardConfiguration
        /// with that position and show the player a dialog where he can finish the configuration.
        /// </summary>
        private void OnMouseUp()
        {
            if (MainCamera.Camera != null && Raycasting.RaycastAnything(out RaycastHit hit))
            {
                position = hit.point;
                position .y += boardPrefab.transform.position.y;
                gotPosition = true;
            }
        }

        internal static void Init()
        {
            boardPrefab = Resources.Load<GameObject>("Prefabs/HolisticMetrics/SceneComponents/MetricsBoard");
            positioningDone = false;
            GameObject.Find("/DemoWorld/Plane").AddComponent<BoardAdder>();
        }
        
        /// <summary>
        /// This component deletes itself once a left click has been registered by any BoardAdder instance.
        /// </summary>
        private void Update()
        {
            if (positioningDone)
            {
                Destroy(this);
            }
        }

        internal static bool GetPosition(out Vector3 clickPosition)
        {
            if (gotPosition)
            {
                clickPosition = position;
                gotPosition = false;
                return true;
            }

            clickPosition = Vector3.zero;
            return false;
        }
        
        internal static void Stop()
        {
            positioningDone = true;
        }
    }
}