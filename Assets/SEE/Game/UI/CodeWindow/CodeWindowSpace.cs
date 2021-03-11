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
        /// <exception cref="ArgumentException">If the given <paramref name="window"/> is already open.</exception>
        /// <exception cref="ArgumentNullException">If the given <paramref name="window"/> is <c>null</c>.</exception>
        public void AddCodeWindow(CodeWindow window)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            } 
            else if (codeWindows.Contains(window))
            {
                throw new ArgumentException("Given window is already open.");
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
        
        /// <summary>
        /// The actual active code window.
        /// </summary>
        private CodeWindow currentActiveCodeWindow;

        /// <summary>
        /// The nominal active code window. Changes will be applied on every frame using <see cref="Update"/>.
        /// TODO: Allow multiple active code windows
        /// </summary>
        public CodeWindow ActiveCodeWindow { get; set; }
    }
}