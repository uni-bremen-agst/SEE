using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.UI.LiveDocumentation
{
    /// <summary>
    /// Part of LiveDocumentationWindow.cs
    ///
    /// Responsible for rendering the Live Documentation on Desktop Machines.
    /// </summary>
    public partial class LiveDocumentationWindow
    {
        
        /// <summary>
        /// Path inside the prefab project tree pointing to a textfield.
        /// In this Text field the name of the class should be displayed.
        /// </summary>
        private const string ClassNameTextFieldPath = "Canvas/ClassName"; 
        
        
        
        /// <summary>
        /// Unity GameObject representing the LiveDocumentation  UI Canvas 
        /// </summary>
        public GameObject documentationWindow { get; private set; }

        
        public Vector2 Resolution = new Vector2(900, 500);

        protected override void StartDesktop()
        {
            documentationWindow = PrefabInstantiator.InstantiatePrefab(PREFAB_PATH, Canvas.transform, true);

            // Set resolution to preferred values
            if (documentationWindow.TryGetComponentOrLog(out RectTransform rect))
            {
                rect.sizeDelta = Resolution;
            }

            // Position code window in center of screen
            documentationWindow.transform.localPosition = Vector3.zero;        
            documentationWindow.SetActive(true);
        }


        /// <summary>
        /// When the documentation window should be started in Desktop Mode
        /// </summary>
        protected override void UpdateDesktop()
        {

        }
    }
}