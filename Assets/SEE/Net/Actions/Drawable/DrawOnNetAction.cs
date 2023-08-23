using Assets.SEE.Game;
using SEE.Game;
using SEE.Net.Actions;
using System.Collections;
using UnityEngine;

namespace Assets.SEE.Net.Actions.Whiteboard
{
    public class DrawOnNetAction : AbstractNetAction
    {
        public string DrawableID;
        public string ParentDrawableID;
        public string Name;
        public Vector3[] Positions;
        public Color Color;
        public float Thickness;
        public int OrderInLayer = -1;
        public Vector3 Position;
        public Vector3 EulerAngles;

        public DrawOnNetAction(
            string drawableID, string parentDrawableID, string name, Vector3[] positions, Color color, float thickness, int orderInLayer)
        {
            this.DrawableID = drawableID;
            this.ParentDrawableID = parentDrawableID;
            this.Name = name;
            this.Positions = positions;
            this.Color = color;
            this.Thickness = thickness;
            this.OrderInLayer = orderInLayer;
            Position = Vector3.zero;
            EulerAngles = Vector3.zero;
        }

        public DrawOnNetAction(
            string drawableID, string parentDrawableID, string name, Vector3[] positions, Color color, float thickness)
        {
            this.DrawableID = drawableID;
            this.ParentDrawableID = parentDrawableID;
            this.Name = name;
            this.Positions = positions;
            this.Color = color;
            this.Thickness = thickness;
        }

        public DrawOnNetAction(
            string drawableID, string parentDrawableID, string name, Vector3[] positions, Color color, float thickness, int orderInLayer, Vector3 position, Vector3 eulerAngles)
        {
            this.DrawableID = drawableID;
            this.ParentDrawableID = parentDrawableID;
            this.Name = name;
            this.Positions = positions;
            this.Color = color;
            this.Thickness = thickness;
            this.OrderInLayer = orderInLayer;
            this.Position = position;
            this.EulerAngles = eulerAngles;
        }

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameDrawableFinder.Find(DrawableID, ParentDrawableID);

                if (drawable == null)
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID}.");
                }
                if (OrderInLayer == -1)
                {
                    GameDrawer.DrawLine(drawable, Name, Positions, Color, Thickness);
                } else
                {
                    if (Position == Vector3.zero && EulerAngles == Vector3.zero)
                    {
                        GameDrawer.ReDrawLine(drawable, Name, Positions, Color, Thickness, OrderInLayer);
                    } else
                    {
                        GameDrawer.ReDrawLine(drawable, Name, Positions, Color, Thickness, OrderInLayer, Position, EulerAngles);
                    }
                }
            }

        }

        protected override void ExecuteOnServer()
        {
        }
    }
}