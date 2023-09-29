using System.Collections;
using System.IO;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
{    
     /// <summary>
     /// This class can be used to load and save the drawables.
     /// </summary>
    public static class DrawableConfigManager
    {
        /// <summary>
        /// The path to the folder containing the saved drawables. This is saved in a field because multiple
        /// methods of this class and other classes use it.
        /// </summary>
        public static readonly string drawablePath = Application.persistentDataPath + "/Drawable/";

        /// <summary>
        /// This method checks whether the directory for the saved drawable exists. If not, then it creates
        /// that directory.
        /// </summary>
        public static void EnsureDrawableDirectoryExists()
        {
            if (!Directory.Exists(drawablePath))
            {
                Directory.CreateDirectory(drawablePath);
            }
        }
    }
}