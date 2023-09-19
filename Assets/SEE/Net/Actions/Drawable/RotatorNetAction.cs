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
        public Vector3 Direction;
        public float Degree;
        public Vector3 FirstPoint;
        public Vector3 HolderPosition;
        public Vector3 ObjectPosition;

        /// <summary>
        /// Creates a new FastEraseNetAction.
        /// </summary>
        /// <param name="gameObjectID">the unique name of the gameObject of a line
        /// that has to be deleted</param>
        public RotatorNetAction(string drawableID, string parentDrawableID, string objectName, Vector3 direction, float degree, Vector3 firstPoint) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            ObjectName = objectName;
            Direction = direction;
            Degree = degree;
            FirstPoint = firstPoint;
        }
        
        public RotatorNetAction(string drawableID, string parentDrawableID, string objectName, float localEulerAnlgeZ, Vector3 holderPosition, Vector3 objectPosition) : base()
        {
            DrawableID = drawableID;
            ParentDrawableID = parentDrawableID;
            ObjectName = objectName;
            Degree = localEulerAnlgeZ;
            HolderPosition = holderPosition;
            ObjectPosition = objectPosition;
            Direction = Vector3.zero;
        }
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
                        GameObject child = GameDrawableFinder.FindChild(drawable, ObjectName);
                        if (Direction != Vector3.zero)
                        {
                            GameMoveRotator.RotateObject(child, Direction, Degree, FirstPoint);
                        } else
                        {
                            GameMoveRotator.SetRotate(child, Degree, HolderPosition, ObjectPosition);
                        }
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