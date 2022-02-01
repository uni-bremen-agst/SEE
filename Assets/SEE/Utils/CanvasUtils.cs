using System;
using System.Linq;
using UnityEngine;

namespace SEE.Utils
{
    public static class CanvasUtils
    {
        /// <summary>
        /// Name of the canvas on which UI elements are placed.
        /// Note that for HoloLens, the canvas will be converted to an MRTK canvas.
        /// </summary>
        private const string UI_CANVAS_NAME = "UI Canvas";

        /// <summary>
        /// Path to where the UI Canvas prefab is stored.
        /// This prefab should contain all components necessary for the UI canvas, such as an event system,
        /// a graphic raycaster, etc.
        /// </summary>
        private const string UI_CANVAS_PREFAB = "Prefabs/UI/UICanvas";

        /// <summary>
        /// Finds the GameObject Canvas.
        /// This GameObject must be named <see cref="UI_CANVAS_NAME"/>.
        /// If it doesn't exist yet, it will be created from a prefab.
        /// </summary>
        /// <returns>The existing/new Canvas</returns>
        public static GameObject GetCanvas()
        {
            GameObject Canvas = GameObject.Find(UI_CANVAS_NAME);
            if (Canvas == null)
            {
                // Create Canvas from prefab if it doesn't exist yet
                Canvas = PrefabInstantiator.InstantiatePrefab(UI_CANVAS_PREFAB);
                Canvas.name = UI_CANVAS_NAME;
            }
            return Canvas;
        }
        
    }
}
