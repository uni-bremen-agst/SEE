using System.IO;
using SEE.Game.UI.HolisticMetrics;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.HolisticMetrics
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
        private static bool addingDone;

        /// <summary>
        /// The configuration of the board to be created. We should be getting this from the AddBoardDialog and we
        /// expect it to only contain the name so far. In this component, the position will be added to it. Then we will
        /// pass it on to the rotation dialog where the rotation will be added.
        /// </summary>
        private static BoardConfiguration boardConfiguration;

        /// <summary>
        /// This is only used to know the height at which to create the new board, because the prefab has a certain
        /// height so it is exactly over the ground.
        /// </summary>
        private static GameObject boardPrefab;
        
        /// <summary>
        /// This sets up all BoardAdders when they are added to the scene to position a new metrics board.
        /// </summary>
        /// <param name="boardConfigurationReference"></param>
        internal static void Setup(BoardConfiguration boardConfigurationReference)
        {
            boardConfiguration = boardConfigurationReference;
            string pathToBoard = Path.Combine("Prefabs", "HolisticMetrics", "SceneComponents", "MetricsBoard");
            boardPrefab = Resources.Load<GameObject>(pathToBoard);
            addingDone = false;
        }
        
        /// <summary>
        /// When the GameObject registers a mouse click, we get the position of the hit, create a new BoardConfiguration
        /// with that position and show the player a dialog where he can finish the configuration.
        /// </summary>
        private void OnMouseUp()
        {
            if (Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Vector3 newPosition = hit.point;
                    newPosition.y += boardPrefab.transform.position.y;

                    boardConfiguration.Position = newPosition;

                    
                    string pathToSlider = Path.Combine("Prefabs", "UI", "MetricsBoardRotation");
                    PrefabInstantiator.InstantiatePrefab(
                            pathToSlider, 
                            GameObject.Find("UI Canvas").transform, 
                            instantiateInWorldSpace: false)
                        .GetComponent<AddBoardSliderController>()
                        .Setup(boardConfiguration);
                    addingDone = true;
                }
            }
        }

        /// <summary>
        /// This component deletes itself once a left click has been registered by any BoardAdder instance.
        /// </summary>
        private void Update()
        {
            if(addingDone)
            {
                Destroy(this);
            }
        }
    }
}