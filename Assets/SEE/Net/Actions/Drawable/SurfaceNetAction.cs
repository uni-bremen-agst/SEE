using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.Net.Actions;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Net.Actions.Drawable
{
    /// <summary>
    /// Superclass for all surface net actions.
    /// It clears the list of drawable types of the surface to make
    /// the configuration smaller.
    /// </summary>
    public class SurfaceNetAction : AbstractNetAction
    {
        /// <summary>
        /// The configuration without drawable type objects
        /// </summary>
        public DrawableConfig DrawableConfig;

        /// <summary>
        /// The drawable object that should be manipulated by this action.
        /// Will be set in the <see cref="ExecuteOnClient"/> method. Can be null.
        /// </summary>
        protected GameObject Surface { get; private set;}

        /// <summary>
        /// The id of the drawable on which the object is located
        /// </summary>
        public string SurfaceID;
        /// <summary>
        /// The id of the drawable surface parent
        /// </summary>
        public string SurfaceParentID;

        /// <summary>
        /// The constructor of this action.
        /// It clears the drawable type object lists.
        /// </summary>
        /// <param name="config">The drawable configuration.</param>
        public SurfaceNetAction(DrawableConfig config) 
        { 
            DrawableConfig = config;
            DrawableConfig.LineConfigs.Clear();
            DrawableConfig.TextConfigs.Clear();
            DrawableConfig.ImageConfigs.Clear();
            DrawableConfig.MindMapNodeConfigs.Clear();
            
        }

        public override void ExecuteOnClient()
        {
            Surface = GameFinder.FindDrawableSurface(DrawableConfig.ID, DrawableConfig.ParentID);
        }
    }
}