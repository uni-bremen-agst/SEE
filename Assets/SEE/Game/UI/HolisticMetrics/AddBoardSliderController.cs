using Michsky.UI.ModernUIPack;
using SEE.Game.HolisticMetrics;
using SEE.Net.Actions.HolisticMetrics;
using UnityEngine;

namespace SEE.Game.UI.HolisticMetrics
{
    /// <summary>
    /// This component is attached to the GameObject that is a menu with a slider and a button and that lets the player
    /// rotate a dummy metrics board and then, once done rotating, create the board.
    /// </summary>
    internal class AddBoardSliderController : MonoBehaviour
    {
        /// <summary>
        /// The slider object. When its value changes (meaning the player moved the slider), the dummy board rotation
        /// will be set to the value of this slider.
        /// </summary>
        [SerializeField] private SliderManager slider;

        /// <summary>
        /// The window dragger for the movable window. We need to tell this dragger the area in which it can be dragged.
        /// </summary>
        [SerializeField] private WindowDragger windowDragger;

        /// <summary>
        /// The board configuration of the board to be created. So far it should already have a title and a position. In
        /// this component, we will add the rotation to it and then create it.
        /// </summary>
        private BoardConfiguration boardConfiguration;

        /// <summary>
        /// The dummy board GameObject that we will instantiate from this component. Its rotation will determine the
        /// rotation of the new board.
        /// </summary>
        private GameObject dummyBoard;
        
        /// <summary>
        /// Sets up this component with the board configuration of the new board and a reference to the boards manager.
        /// Also sets up the slider, the window dragger and instantiates the dummy board.
        /// </summary>
        /// <param name="boardConfigurationReference">The BoardConfiguration for the board to be created</param>
        internal void Setup(BoardConfiguration boardConfigurationReference)
        {
            boardConfiguration = boardConfigurationReference;

            windowDragger.dragArea = transform.parent.GetComponent<RectTransform>();
            
            slider.mainSlider.onValueChanged.AddListener(Rotate);
            const string pathToBoard = "Prefabs/HolisticMetrics/SceneComponents/MetricsBoard";
            GameObject boardPrefab = Resources.Load<GameObject>(pathToBoard);
            dummyBoard = Instantiate(boardPrefab, boardConfiguration.Position, Quaternion.identity);
        }
        
        /// <summary>
        /// This method will be called when the slider value changes. It will change the dummy board's rotation to the
        /// slider's value.
        /// </summary>
        /// <param name="yAxisRotation">The new rotation value. This comes from the slider.</param>
        private void Rotate(float yAxisRotation)
        {
            Quaternion rotation = Quaternion.identity;
            rotation.eulerAngles = new Vector3(0, yAxisRotation, 0);
            dummyBoard.transform.rotation = rotation;
        }

        /// <summary>
        /// Will be called by the button when it is being pressed. This will finally create the new board and then
        /// delete the GameObject this script is attached to and the dummy board.
        /// </summary>
        public void CreateBoard()
        {
            boardConfiguration.Rotation = dummyBoard.transform.rotation;
            Destroy(dummyBoard);
            BoardsManager.Create(boardConfiguration);
            new CreateBoardNetAction(boardConfiguration).Execute();
            Destroy(gameObject);
        }
    }
}
