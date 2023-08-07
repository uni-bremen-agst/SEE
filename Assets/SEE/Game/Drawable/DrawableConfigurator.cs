using SEE.Game;
using SEE.GO;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
{
    public static class DrawableConfigurator
    {
        /// <summary>
        /// The current choosen color for drawing.
        /// </summary>
        public static Color currentColor { get; set; }

        /// <summary>
        /// The current choosen thickness for drawing.
        /// </summary>
        public static float currentThickness { get; set; }

        public static int orderInLayer { get; set; }

        static DrawableConfigurator() {
            currentColor = UnityEngine.Random.ColorHSV();
            currentThickness = 0.01f;
            orderInLayer = 0;            
        }
    }
}