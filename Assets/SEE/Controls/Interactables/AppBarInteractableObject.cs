using System;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.UI.BoundsControlTypes;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls
{
    /// <summary>
    /// GameObjects with this component will display an AppBar when clicked at by a mixed reality hand pointer.
    /// </summary>
    public class AppBarInteractableObject : MonoBehaviour, IMixedRealityPointerHandler
    {
        /// <summary>
        /// Custom MixedReality pointed action for current game object
        /// </summary>
        private Action<GameObject> MixedRealityPointedAction;

        /// <summary>
        /// AppBar showing when pointed at this game object.
        /// </summary>
        private GameObject AppBar;

        /// <summary>
        /// Bounds
        /// </summary>
        private BoundsControl BoundsControl;

        /// <summary>
        /// Whether an app bar should be shown at all.
        /// </summary>
        private bool ShowAppBar = true;

        /// <summary>
        /// Whether an app bar should be created if it doesn't exist yet.
        /// </summary>
        public bool CreateAppBar = false;
        
        /// <summary>
        /// The app bar containing actions.
        /// </summary>
        private AppBar AppBarComponent;

        private void Start()
        {

            if (PlayerSettings.GetInputType() != PlayerSettings.PlayerInputType.HoloLens)
            {
                Destroyer.DestroyComponent(this);
                return;
            }

            AppBar = GameObject.Find("MainAppBar");

            if (AppBar == null && CreateAppBar)
            {
                GameObject appBar = new GameObject("MainAppBar", typeof(AppBar));
                appBar.transform.parent = null;
                AppBar = appBar;

                Debug.Log($"A new AppBar '{AppBar.name}' was created and added to root into the current scene");
            }
            else if (!AppBar.TryGetComponentOrLog(out AppBarComponent))
            {
                AppBar.AddComponent<AppBar>();
                AppBarComponent = AppBar.GetComponent<AppBar>();
            }

            if (!gameObject.TryGetComponentOrLog(out BoundsControl))
            {
                gameObject.AddComponent<BoundsControl>();
                BoundsControl = gameObject.GetComponent<BoundsControl>();
                Debug.Log($"A BoundsControl was added to the GameObject {gameObject.name}!");
                BoundsControl.BoundsControlActivation = BoundsControlActivationType.ActivateByPointer;

            }
        }

        /// <summary>
        /// Changes the action for this game object when it were pointed 
        /// </summary>
        /// <param name="SelectionAction">
        /// The action which shall be executed when the game object is pointed at.
        /// </param>
        public void SetPointedAction(Action<GameObject> SelectionAction, bool showAppBar = true)
        {
            MixedRealityPointedAction = SelectionAction;
            ShowAppBar = showAppBar;
        }

        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {
            PerformPointedAction();
        }

        public void OnPointerDragged(MixedRealityPointerEventData eventData)
        {
            // Intentionally left blank.
        }

        public void OnPointerUp(MixedRealityPointerEventData eventData)
        {
            // Intentionally left blank.
        }

        public void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Action when GameObject was pointed at.
        /// </summary>
        private void PerformPointedAction()
        {
            ShowGameObjectAppBar();
            MixedRealityPointedAction?.Invoke(gameObject);
        }

        /// <summary>
        /// Displays an AppBar on the game object.
        /// </summary>
        private void ShowGameObjectAppBar()
        {
            AppBarComponent.Target = BoundsControl;
        }
    }
}
