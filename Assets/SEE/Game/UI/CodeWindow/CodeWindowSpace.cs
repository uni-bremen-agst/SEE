using System;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Game.UI.CodeWindow
{
    /// <summary>
    /// An ordered collection of active code windows.
    /// The user will be able to choose which of the currently active code windows will be shown.
    /// This component is responsible for arranging, displaying, and hiding the code windows.
    /// </summary>
    public partial class CodeWindowSpace: PlatformDependentComponent
    {
        /// <summary>
        /// Returns a <b>read-only wrapper</b> around the list of active code windows.
        /// </summary>
        public IList<CodeWindow> CodeWindows => codeWindows.AsReadOnly();
        
        /// <summary>
        /// Path to the code window space prefab, in which all code windows will be contained.
        /// </summary>
        private const string CODE_WINDOW_SPACE_PREFAB = "Prefabs/UI/CodeWindowSpace";
        
        /// <summary>
        /// Name of the game object containing the code windows.
        /// </summary>
        private const string CODE_WINDOW_SPACE_NAME = "CodeWindowSpace";
        
        /// <summary>
        /// A list of all nominal active code windows. May be empty.
        /// </summary>
        private readonly List<CodeWindow> codeWindows = new List<CodeWindow>();

        /// <summary>
        /// A list of all actual active code windows. May be empty.
        /// </summary>
        private readonly List<CodeWindow> currentCodeWindows = new List<CodeWindow>();

        /// <summary>
        /// The game object on the UI canvas. This object will contain all UI code windows.
        /// </summary>
        private GameObject space;

        /// <summary>
        /// Adds a new code window to the list of active code windows.
        /// </summary>
        /// <param name="window">The active code window which should be added to the list.</param>
        /// <param name="splitOff">Will create an own gameObject for the code window and parent it to that.
        /// This CodeWindowSpace will then be responsible for maintaining the new gameObject, hence it will
        /// also be parented to this component's gameObject.</param>
        /// <exception cref="ArgumentException">If the given <paramref name="window"/> is already open.</exception>
        /// <exception cref="ArgumentNullException">If the given <paramref name="window"/> is <c>null</c>.</exception>
        public void AddCodeWindow(CodeWindow window, bool splitOff = false)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            } 
            else if (codeWindows.Contains(window))
            {
                throw new ArgumentException("Given window is already open.");
            }
            
            // Split the code window off into its own game object
            if (splitOff)
            {
                GameObject windowGameObject = new GameObject {name = window.Title};
                windowGameObject.transform.parent = gameObject.transform;
            }

            codeWindows.Add(window);
            // Actual UI generation happens in Update()
        }

        /// <summary>
        /// Closes a previously opened code window.
        /// </summary>
        /// <param name="window">The code window which should be closed.</param>
        /// <exception cref="ArgumentException">If the given <paramref name="window"/> is already closed.</exception>
        /// <exception cref="ArgumentNullException">If the given <paramref name="window"/> is <c>null</c>.</exception>
        public void CloseCodeWindow(CodeWindow window)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            } else if (!codeWindows.Contains(window))
            {
                throw new ArgumentException("Given window is already closed.");
            }
            codeWindows.Remove(window);
        }

        public void OnDisable()
        {
            // TODO: When disabled, all code windows need to be disabled and hidden by disabling their game object.
            // This should only be done if we are a parent of them, otherwise we'd have to interfere with external
            // gameObjects.
            // TODO: Similarly, OnEnable should be implemented.
        }

        /// <summary>
        /// The actual active code window.
        /// </summary>
        private CodeWindow currentActiveCodeWindow;

        /// <summary>
        /// The nominal active code window. Changes will be applied on every frame using <see cref="Update"/>.
        /// </summary>
        public CodeWindow ActiveCodeWindow { get; set; }
    }
}