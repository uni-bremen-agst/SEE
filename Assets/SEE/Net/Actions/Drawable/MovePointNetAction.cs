using SEE.Controls.Actions.Drawable;
using SEE.Game.Drawable;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for changing positions of a given line (<see cref="MovePointAction"/>) on all clients.
    /// </summary>
    public class MovePointNetAction : DrawableNetAction
    {
        /// <summary>
        /// The ID of the line thats line renderer positions should be changed
        /// </summary>
        public string LineName;
        /// <summary>
        /// The new position for the selected positions
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// The selected positions
        /// </summary>
        public List<int> Indices;

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The ID of the drawable on which the line is located.</param>
        /// <param name="parentDrawableID">The ID of the drawable parent.</param>
        /// <param name="lineName">The ID of the line thats line renderer positions should be changed.</param>
        /// <param name="Indices">The selected positions of the line renderer.</param>
        /// <param name="position">The new position that should be set for the selected positions.</param>
        public MovePointNetAction(string drawableID, string parentDrawableID, string lineName, List<int> indices, Vector3 position)
            : base(drawableID, parentDrawableID)
        {
            LineName = lineName;
            Position = position;
            Indices = indices;
        }

        /// <summary>
        /// Changes the position of the selected positions from the line renderer of the given line.
        /// </summary>
        /// <exception cref="System.Exception">Will be thrown, if the <see cref="DrawableID"/> or <see cref="LineName"/> don't exists.</exception>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameMoveRotator.MovePoint(FindChild(LineName), Indices, Position);
        }
    }
}