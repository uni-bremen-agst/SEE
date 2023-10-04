using Assets.SEE.Game.Drawable;
using Assets.SEE.Game;
using SEE.Net.Actions;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using SEE.Controls.Actions.Drawable;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing positions of a given line (<see cref="MovePointAction"/>) of an object on all clients.
    /// </summary>
    public class MovePointNetAction : AbstractNetAction
    {
        /// <summary>
        /// The id of the drawable on which the object is located
        /// </summary>
        public string DrawableID;
        /// <summary>
        /// The id of the drawable parent
        /// </summary>
        public string ParentDrawableID;
        /// <summary>
        /// The id of the line thats line renderer positions should be changed
        /// </summary>
        public string LineName;
        /// <summary>
        /// The new position for the selected positions
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// The selected positions
        /// </summary>
        public List<int> Indexes;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="lineName">The id of the line thats line renderer positions should be changed</param>
        /// <param name="indexes">The selected positions of the line renderer</param>
        /// <param name="position">The new position that should be set for the selected positions.</param>
        public MovePointNetAction(string drawableID, string parentDrawableID, string lineName, List<int> indexes, Vector3 position) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            LineName = lineName;
            this.Position = position;
            this.Indexes = indexes;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {

        }

        /// <summary>
        /// Changes the position of the selected positions from the line renderer of the given line.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="LineName"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                if (!IsRequester())
                {
                    GameObject drawable = GameDrawableFinder.Find(DrawableID, ParentDrawableID);
                    if (drawable != null && GameDrawableFinder.FindChild(drawable, LineName) != null)
                    {
                        GameMoveRotator.MovePoint(GameDrawableFinder.FindChild(drawable, LineName), Indexes, Position);
                    }
                    else
                    {
                        throw new System.Exception($"There is no drawable with the ID {DrawableID} or line with the ID {LineName}.");
                    }
                }
            }
        }
    }
}