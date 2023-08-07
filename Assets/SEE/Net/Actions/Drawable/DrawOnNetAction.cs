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

        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameDrawableIDFinder.Find(DrawableID, ParentDrawableID);

                if (drawable == null)
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID}.");
                }
                if (OrderInLayer == -1)
                {
                    GameDrawer.DrawLine(drawable, Name, Positions, Color, Thickness);
                } else
                {
                    GameDrawer.ReDrawLine(drawable, Name, Positions, Color, Thickness, OrderInLayer);
                }
            }

        }

        protected override void ExecuteOnServer()
        {
        }
    }
}