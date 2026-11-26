using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace SEE.UI.Window
{
    /// <summary>
    /// Manages an ordered collection of windows and their active selection.
    /// Windows are registered and removed through the public API, while
    /// their visual representation is synchronized during subsequent updates.
    /// The platform-specific implementation arranges the windows and
    /// manages their visibility and interaction.
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
        /// Invoked immediately after a window is registered with this
        /// window space. Its visual representation may not yet exist,
        /// because window initialization is processed during updates.
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
        /// <exception cref="ArgumentNullException">If the given <paramref name="window"/> is null.</exception>
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
        /// <exception cref="ArgumentNullException">If the given <paramref name="window"/> is null.</exception>
        /// <returns>True if the window was closed, false otherwise.</returns>
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
        /// Gets or sets the nominally active window.
        /// Changes to this property are synchronized with the corresponding
        /// visual tab during a subsequent update.
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
        /// Re-creates a <see cref="WindowSpace"/> from the given
        /// <paramref name="valueObject"/> and attaches it to
        /// <paramref name="attachTo"/>.
        /// </summary>
        /// <param name="valueObject">
        /// The serialized window-space values to restore.
        /// </param>
        /// <param name="attachTo">
        /// The GameObject to which the new window space is attached.
        /// </param>
        /// <param name="attachWindows">
        /// Whether all restored windows should also be attached to
        /// <paramref name="attachTo"/> instead of their individually
        /// specified GameObjects.
        /// </param>
        /// <returns>The restored window space.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="attachTo"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if a required window cannot be restored or the serialized
        /// active window cannot be found among the restored windows.
        /// </exception>
        public static WindowSpace FromValueObject(WindowSpaceValues valueObject, GameObject attachTo,
                                                  bool attachWindows = false)
        {
            if (attachTo == null)
            {
                throw new ArgumentNullException(nameof(attachTo));
            }

            WindowSpace space = attachTo.AddComponent<WindowSpace>();

            space.windows.AddRange(valueObject.Windows.Select(
                x => BaseWindow.FromValueObject<BaseWindow>(
                    x, attachWindows ? attachTo : null)));

            if (valueObject.HasActiveWindow)
            {
                if (valueObject.ActiveWindow == null)
                {
                    throw new InvalidOperationException("The serialized window space has no active window values.");
                }

                space.ActiveWindow = space.windows.First(
                    window => window.Title == valueObject.ActiveWindow.Title);
            }
            else
            {
                space.ActiveWindow = null;
            }

            return space;
        }

        /// <summary>
        /// Creates a serializable representation of this window space,
        /// including its registered windows and optional active window.
        /// </summary>
        /// <returns>
        /// The serialized values representing the current window space.
        /// An empty window space is represented without an active window.
        /// </returns>
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
            /// Whether this window space contains an active window.
            /// An empty window space has no active window.
            /// </summary>
            [field: SerializeField]
            public bool HasActiveWindow { get; private set; }

            /// <summary>
            /// Generated value object of the active window in this space.
            /// Only valid if <see cref="HasActiveWindow"/> is true.
            /// </summary>
            [field: SerializeField]
            public WindowValues ActiveWindow { get; private set; }

            /// <summary>
            /// A list of generated value objects for each window in this space.
            /// </summary>
            [field: SerializeField]
            public List<WindowValues> Windows { get; private set; }

            /// <summary>
            /// Creates a new <see cref="WindowSpaceValues"/> object from the
            /// given parameters.
            /// Creates a value object for every window in the space.
            /// </summary>
            /// <param name="windows">
            /// The windows belonging to this space.
            /// </param>
            /// <param name="activeWindow">
            /// The currently active window, or null if no window is active.
            /// </param>
            internal WindowSpaceValues(IEnumerable<BaseWindow> windows, BaseWindow activeWindow)
            {
                HasActiveWindow = activeWindow != null;
                ActiveWindow = HasActiveWindow ? activeWindow.ToValueObject() : null;
                Windows = windows.Select(window => window.ToValueObject()).ToList();
            }
        }

        #endregion
    }
}
