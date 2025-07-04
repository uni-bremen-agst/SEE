using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace SEE.UI.Window
{
    /// <summary>
    /// An ordered collection of active windows.
    /// The user will be able to choose which of the currently active windows will be shown.
    /// This component is responsible for arranging, displaying, and hiding the windows.
    /// </summary>
    public partial class WindowSpace : PlatformDependentComponent
    {
        /// <summary>
        /// Returns a <b>read-only wrapper</b> around the list of active windows.
        /// </summary>
        public IList<BaseWindow> Windows => windows.AsReadOnly();

        /// <summary>
        /// Path to the code space prefab, in which all windows will be contained.
        /// </summary>
        private const string windowSpacePrefab = "Prefabs/UI/WindowSpace";

        /// <summary>
        /// Name of the game object containing the windows.
        /// Can only be changed before <see cref="Start"/> has been called.
        /// </summary>
        [FormerlySerializedAs("CodeSpaceName")]
        public string WindowSpaceName = "WindowSpace";

        /// <summary>
        /// This event will be invoked whenever a new window is added to the space.
        /// </summary>
        public UnityEvent<BaseWindow> OnWindowAdded = new();

        /// <summary>
        /// Whether to allow the user to close the tabs containing the windows.
        /// Changing this value will only have an effect when doing so before <see cref="Start"/> has been called.
        /// </summary>
        public bool CanClose = true;

        /// <summary>
        /// A list of all nominal active windows. May be empty.
        /// </summary>
        [ManagedUI(toggleEnabled: true, destroy: false)]
        private readonly List<BaseWindow> windows = new();

        /// <summary>
        /// A list of all actual active windows. May be empty.
        /// </summary>
        private readonly List<BaseWindow> currentWindows = new();

        /// <summary>
        /// The game object on the UI canvas. This object will contain all UI windows.
        /// </summary>
        [ManagedUI(toggleEnabled: true, destroy: false)]
        private GameObject space;

        /// <summary>
        /// Adds a new window to the list of active windows.
        /// </summary>
        /// <param name="window">The active window which should be added to the list.</param>
        /// <exception cref="ArgumentException">If the given <paramref name="window"/> is already open.</exception>
        /// <exception cref="ArgumentNullException">If the given <paramref name="window"/> is <c>null</c>.</exception>
        public void AddWindow(BaseWindow window)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }
            else if (windows.Contains(window))
            {
                throw new ArgumentException("Given window is already open.");
            }
            else if (windows.Find(x => x.Title == window.Title))
            {
                Debug.LogError("Warning: Multiple windows with the same title are in the same space. "
                               + "This will lead to issues when syncing across the network.\n");
            }

            windows.Add(window);
            OnWindowAdded.Invoke(window);
            // Actual UI generation happens in Update()
        }

        /// <summary>
        /// Closes a previously opened window.
        /// </summary>
        /// <param name="window">The window which should be closed.</param>
        /// <exception cref="ArgumentNullException">If the given <paramref name="window"/> is <c>null</c>.</exception>
        /// <returns><c>true</c> if the window was closed, <c>false</c> otherwise.</returns>
        public bool CloseWindow(BaseWindow window)
        {
            if (ReferenceEquals(window, null))
            {
                throw new ArgumentNullException(nameof(window));
            }
            return windows.Remove(window);
        }

        /// <summary>
        /// The actual active window.
        /// </summary>
        private BaseWindow currentActiveWindow;

        /// <summary>
        /// The nominal active window. Changes will be applied on every frame using <see cref="Update"/>.
        /// </summary>
        public BaseWindow ActiveWindow { get; set; }

        /// <summary>
        /// This event will be invoked whenever the active window is changed.
        /// This includes changing the active window to nothing (i.e. closing all of them.)
        /// </summary>
        [FormerlySerializedAs("OnActiveCodeWindowChanged")]
        public UnityEvent OnActiveWindowChanged = new();

        #region Value Object

        /// <summary>
        /// Re-creates a <see cref="WindowSpace"/> from the given <paramref name="valueObject"/> and attaches
        /// it to the given GameObject <paramref name="attachTo"/>.
        /// </summary>
        /// <param name="valueObject">The value object from which the space should be constructed.</param>
        /// <param name="attachTo">The game object to which the new space should be attached.</param>
        /// <param name="attachWindows">Whether the <see cref="WindowValues"/> in the space should
        /// be attached to <see cref="attachTo"/> as well, instead of the GameObject that might be defined
        /// in them.</param>
        /// <returns>The newly re-created <see cref="WindowSpace"/>.</returns>
        public static WindowSpace FromValueObject(WindowSpaceValues valueObject, GameObject attachTo,
                                                  bool attachWindows = false)
        {
            if (attachTo == null)
            {
                throw new ArgumentNullException(nameof(attachTo));
            }

            WindowSpace space = attachTo.AddComponent<WindowSpace>();
            space.windows.AddRange(valueObject.Windows.Select(x => BaseWindow.FromValueObject<BaseWindow>(x, attachWindows ? attachTo : null)));
            space.ActiveWindow = space.windows.First(x => valueObject.ActiveWindow.Title == x.Title);
            return space;
        }

        /// <summary>
        /// Generates and returns a <see cref="WindowSpaceValues"/> struct for this space.
        /// </summary>
        /// <returns>The newly created <see cref="WindowSpaceValues"/>, matching this class</returns>
        public WindowSpaceValues ToValueObject()
        {
            return new WindowSpaceValues(Windows, ActiveWindow);
        }

        /// <summary>
        /// Represents the values of a window space necessary to re-create its content.
        /// Used for serialization when sending a <see cref="WindowSpace"/> over the network.
        /// </summary>
        [Serializable]
        public struct WindowSpaceValues
        {
            /// <summary>
            /// Generated value object of the active window in this space.
            /// </summary>
            [field: SerializeField]
            public WindowValues ActiveWindow { get; private set; }

            /// <summary>
            /// A list of generated value objects for each window in this space.
            /// </summary>
            [field: SerializeField]
            public List<WindowValues> Windows { get; private set; }

            /// <summary>
            /// Creates a new <see cref="WindowSpaceValues"/> object from the given parameters.
            /// Note that this will create a <see cref="WindowSpaceValues"/> object for each window in the space.
            /// </summary>
            /// <param name="windows">List of windows in the space.</param>
            /// <param name="activeWindow">Currently active window in the space.</param>
            internal WindowSpaceValues(IEnumerable<BaseWindow> windows, BaseWindow activeWindow)
            {
                ActiveWindow = activeWindow.ToValueObject();
                Windows = windows.Select(x => x.ToValueObject()).ToList();
            }
        }

        #endregion
    }
}
