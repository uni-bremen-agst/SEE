using Michsky.UI.ModernUIPack;
using SEE.Utils;
using UnityEngine;

namespace SEE.UI.HolisticMetrics
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
        /// Whether this class has a rotation in store that wasn't yet fetched.
        /// </summary>
        private static bool gotRotation;

        /// <summary>
        /// If <see cref="gotRotation"/> is true, this contains the board rotation this slider controller got from the
        /// player.
        /// </summary>
        private static Quaternion boardRotation;

        /// <summary>
        /// The dummy board GameObject that we will instantiate from this component. Its rotation will determine the
        /// rotation of the new board.
        /// </summary>
        private GameObject dummyBoard;

        /// <summary>
        /// Sets up this component with the board configuration of the new board and a reference to the boards manager.
        /// Also sets up the slider, the window dragger and instantiates the dummy board.
        /// </summary>
        internal void Setup(Vector3 position)
        {
            windowDragger.dragArea = transform.parent.GetComponent<RectTransform>();

            slider.mainSlider.onValueChanged.AddListener(Rotate);
            const string pathToBoard = "Prefabs/HolisticMetrics/SceneComponents/MetricsBoard";
            GameObject boardPrefab = Resources.Load<GameObject>(pathToBoard);
            dummyBoard = Instantiate(boardPrefab, position, Quaternion.identity);
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
            boardRotation = dummyBoard.transform.rotation;
            gotRotation = true;
            Destroyer.Destroy(dummyBoard);
            Destroyer.Destroy(gameObject);
        }

        /// <summary>
        /// If <see cref="gotRotation"/> is true, the <paramref name="rotation"/> will be the rotation given by the
        /// player. Otherwise it will be some dummy value.
        /// </summary>
        /// <param name="rotation">The rotation the player confirmed, if that doesn't exist, some dummy value.</param>
        /// <returns><see cref="gotRotation"/>.</returns>
        internal static bool TryGetRotation(out Quaternion rotation)
        {
            if (gotRotation)
            {
                rotation = boardRotation;
                gotRotation = false;
                return true;
            }

            rotation = Quaternion.identity;
            return false;
        }
    }
}
