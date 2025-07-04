using System;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI.Window
{
    /// <summary>
    /// Represents a movable window.
    /// </summary>
    public abstract class BaseWindow : PlatformDependentComponent
    {
        /// <summary>
        /// The title (e.g. filename) for the window.
        /// </summary>
        private string title;

        /// <summary>
        /// The title (e.g. filename) for the window.
        /// </summary>
        public string Title
        {
            get => title;
            set
            {
                title = value;
                if (HasStarted)
                {
                    Window.name = Title;
                    Window.transform.Find("Dragger/Title").gameObject.MustGetComponent<TextMeshProUGUI>().text = Title;
                }
            }
        }

        /// <summary>
        /// Resolution of the window.
        /// </summary>
        protected Vector2 Resolution = new(900, 500);

        /// <summary>
        /// GameObject containing the actual UI for the window.
        /// </summary>
        [ManagedUI(toggleEnabled: true, destroy: false)]
        public GameObject Window { get; protected set; }

        /// <summary>
        /// Path to the window canvas prefab.
        /// </summary>
        private const string windowPrefab = "Prefabs/UI/BaseWindow";

        protected override void StartDesktop()
        {
            if (Title == null)
            {
                Debug.LogError("Title must be defined when setting up Window!\n");
                return;
            }

            Window = PrefabInstantiator.InstantiatePrefab(windowPrefab, Canvas.transform, false);
            Window.name = Title;

            // Position code window in center of screen
            Window.transform.localPosition = Vector3.zero;

            // Set resolution to preferred values
            if (Window.TryGetComponentOrLog(out RectTransform rect))
            {
                rect.sizeDelta = Resolution;
            }

            // Set title
            Window.transform.Find("Dragger/Title").gameObject.GetComponent<TextMeshProUGUI>().text = Title;

            /// Disables the window dragger IDE buttons.
            /// Note: If a sub class needs the IDE buttons, call <see cref="ActivateWindowDraggerButtons">
            DisableWindowDraggerButtons();
        }

        /// <summary>
        /// Disables the window dragger buttons.
        /// </summary>
        private void DisableWindowDraggerButtons()
        {
            Button[] buttons = Window.transform.Find("Dragger").GetComponentsInChildren<Button>();
            foreach (Button button in buttons)
            {
                button.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Activates the window dragger buttons.
        /// </summary>
        protected void ActivateWindowDraggerButtons()
        {
            Button[] buttons = Window.transform.Find("Dragger").GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                button.gameObject.SetActive(true);
            }
        }

        protected override void StartVR()
        {
            StartDesktop();
        }

        /// <summary>
        /// Rebuilds any parts of the window which are dependent on its size.
        /// This method will be called whenever the layout of the window changes in any way, for example, if its
        /// size is changed.
        /// </summary>
        public abstract void RebuildLayout();

        /// <summary>
        /// Sets up this newly created window from the values given in the <paramref name="valueObject"/>.
        ///
        /// Note that the <see cref="Title"/> and <c>AttachedTo</c> attributes needn't be handled, only newly added
        /// fields compared to <see cref="WindowValues"/> are relevant here.
        ///
        /// </summary>
        /// <param name="valueObject">The window value object whose values shall be used.</param>
        /// <remarks>
        /// <see cref="Start"/> has not been called at this point.
        /// </remarks>
        protected abstract void InitializeFromValueObject(WindowValues valueObject);

        /// <summary>
        /// Updates this window from the values given in the <paramref name="valueObject"/>, which is a value object
        /// received over the network from another player.
        ///
        /// Note that this method will be called often, hence, do not simply use every new value if that negatively
        /// impedes performance! See below for an example.
        ///
        /// </summary>
        /// <param name="valueObject">The window value object whose updated values shall be used.</param>
        /// <example>
        /// For example, the code windows only take into account the visible line here, which changes when another
        /// player scrolls through the code window and which must always be updated.
        /// It does not take into account things like the title or the path to the source code file, as these cannot
        /// change and would use unnecessary resource to update to (e.g., having to re-read the file).
        /// </example>
        public abstract void UpdateFromNetworkValueObject(WindowValues valueObject);

        /// <summary>
        /// Recreates a window from the given <paramref name="valueObject"/> and attaches it to
        /// the GameObject <paramref name="attachTo"/>.
        /// </summary>
        /// <param name="valueObject">The value object from which the window should be constructed</param>
        /// <param name="attachTo">The game object the window should be attached to. If <c>null</c>,
        /// the game object will be attached to the game object with the name specified in the value object.</param>
        /// <returns>The newly re-constructed window</returns>
        /// <exception cref="InvalidOperationException">If both <paramref name="attachTo"/> is <c>null</c>
        /// and the game object specified in <paramref name="valueObject"/> can't be found.</exception>
        public static BaseWindow FromValueObject<T>(WindowValues valueObject, GameObject attachTo = null)
            where T : BaseWindow
        {
            if (attachTo == null)
            {
                attachTo = GraphElementIDMap.Find(valueObject.AttachedTo);
                if (attachTo == null)
                {
                    throw new InvalidOperationException($"GameObject with name {attachTo} could not be found.\n");
                }
            }

            T window = attachTo.AddComponent<T>();
            window.Title = valueObject.Title;
            window.InitializeFromValueObject(valueObject);
            return window;
        }

        /// <summary>
        /// Generates and returns a value object for this window.
        /// </summary>
        /// <returns>The newly created window value object, matching this class</returns>
        public abstract WindowValues ToValueObject();
    }

    /// <summary>
    /// Represents the values of a window needed to re-create its content.
    /// Used for serialization when sending a window over the network.
    /// </summary>
    [Serializable]
    public class WindowValues
    {
        /// <summary>
        /// Title of the window.
        /// </summary>
        [field: SerializeField]
        public string Title { get; private set; }

        [field: SerializeField]
        /// <summary>
        /// Name of the game object this window was attached to.
        /// </summary>
        public string AttachedTo { get; private set; }

        /// <summary>
        /// Creates a new WindowValues object from the given parameters.
        /// </summary>
        /// <param name="title">The title of the window.</param>
        /// <param name="attachedTo">Name of the game object the code window is attached to.</param>
        internal WindowValues(string title, string attachedTo = null)
        {
            AttachedTo = attachedTo;
            Title = title;
        }
    }
}
