using Assets.SEE.DataModel.Drawable;
using Assets.SEE.Game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.SEE.GameObjects
{
    /// <summary>
    /// 
    /// </summary>
    public class DrawableSurfacesRef : MonoBehaviour
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly DrawableSurfaces surfacesInScene;

        /// <summary>
        /// 
        /// </summary>
        public DrawableSurfaces SurfacesInScene { get { return surfacesInScene; } }
    
        /// <summary>
        /// 
        /// </summary>
        public DrawableSurfacesRef()
        {
            surfacesInScene = new DrawableSurfaces();
        }
    }
}