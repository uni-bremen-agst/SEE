using Assets.SEE.Game.Drawable;
using Assets.SEE.Game;
using SEE.Net.Actions;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Net.Actions.Drawable
{
    public class RotatorNetAction : AbstractNetAction
    {
        public string DrawableID;
        public string ParentDrawableID;
        public string ObjectName;
        //public Quaternion OldRotation;
        public Vector3 FirstPoint;
        public Vector3 Direction;
        public float Degree;
        public Vector3 OldPosition;

        /// <summary>
        /// Creates a new FastEraseNetAction.
        /// </summary>
        /// <param name="gameObjectID">the unique name of the gameObject of a line
        /// that has to be deleted</param>
        public RotatorNetAction(string drawableID, string parentDrawableID, string objectName, Vector3 firstPoint, Vector3 direction, float degree, Vector3 oldPosition) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            ObjectName = objectName;
            FirstPoint = firstPoint;
            Direction = direction;
            Degree = degree;
            OldPosition = oldPosition;
        }
        /*
        public RotatorNetAction(string drawableID, string parentDrawableID, string objectName, Quaternion rotation) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            ObjectName = objectName;
            OldRotation = rotation;
            Degree = -1;
        }*/
        protected override void ExecuteOnServer()
        {

        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                if (!IsRequester())
                {
                    GameObject drawable = GameDrawableFinder.Find(DrawableID, ParentDrawableID);
                    if (drawable != null && GameDrawableFinder.FindChild(drawable, ObjectName) != null)
                    {
                            GameMoveRotator.RotateObject(GameDrawableFinder.FindChild(drawable, ObjectName), FirstPoint, Direction, Degree, OldPosition);
                    }
                    else
                    {
                        throw new System.Exception($"There is no drawable with the ID {DrawableID} or line with the ID {ObjectName}.");
                    }
                }
            }
        }
    }
}