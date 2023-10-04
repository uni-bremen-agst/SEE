using Assets.SEE.Game;
using Assets.SEE.Game.Drawable;
using SEE.Game;
using static SEE.Game.GameDrawer;
using SEE.Controls.Actions.Drawable;
using UnityEngine;

namespace SEE.Net.Actions.Drawable
{
    /// <summary>
    /// This class is responsible for drawing (<see cref="DrawOnAction"/>) a line on the given drawable on all clients.
    /// </summary>
    public class DrawOnNetAction : AbstractNetAction
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
        /// The name of the line that should be drawn
        /// </summary>
        public string Name;
        /// <summary>
        /// The positions of the line for the line renderer.
        /// </summary>
        public Vector3[] Positions;
        /// <summary>
        /// The color of the line.
        /// </summary>
        public Color Color;
        /// <summary>
        /// The thickness of the line.
        /// </summary>
        public float Thickness;
        /// <summary>
        /// The order in layer of the line.
        /// Default -1 means that no order has been specified and the current order should be used.
        /// </summary>
        public int OrderInLayer = -1;
        /// <summary>
        /// The loop, means if the line should loop.
        /// </summary>
        public bool Loop;
        /// <summary>
        /// The line kind of the line.
        /// </summary>
        public GameDrawer.LineKind LineKind;
        /// <summary>
        /// The tiling of the line, only necressary for line kind dashed.
        /// </summary>
        public float Tiling;
        /// <summary>
        /// The line that should be drawn as <see cref="Line"/> object.
        /// </summary>
        public Line Line;

        public DrawOnNetAction(
            string drawableID, string parentDrawableID, string name, Vector3[] positions, Color color, 
            float thickness, int orderInLayer, bool loop, LineKind lineKind, float tiling)
        {
            this.DrawableID = drawableID;
            this.ParentDrawableID = parentDrawableID;
            this.Name = name;
            this.Positions = positions;
            this.Color = color;
            this.Thickness = thickness;
            this.OrderInLayer = orderInLayer;
            this.Loop = loop;
            this.LineKind = lineKind;
            this.Tiling = tiling;
            Line = null;
        }

        public DrawOnNetAction(
            string drawableID, string parentDrawableID, string name, Vector3[] positions, Color color, float thickness, bool loop, LineKind lineKind, float tiling)
        {
            this.DrawableID = drawableID;
            this.ParentDrawableID = parentDrawableID;
            this.Name = name;
            this.Positions = positions;
            this.Color = color;
            this.Thickness = thickness;
            this.Loop = loop;
            this.LineKind = lineKind;
            this.Tiling = tiling;
            Line = null;
        }

        /// <summary>
        /// The constructor of this action. All it does is assign the value you pass it to a field.
        /// </summary>
        /// <param name="drawableID">The id of the drawable on which the object is located.</param>
        /// <param name="parentDrawableID">The id of the drawable parent.</param>
        /// <param name="line">The line that should be drawn.</param>
        public DrawOnNetAction(string drawableID, string parentDrawableID, Line line)
        {
            this.DrawableID = drawableID;
            this.ParentDrawableID = parentDrawableID;
            this.Line = line;
        }

        /// <summary>
        /// Draws the line on each client.
        /// </summary>
        /// <exception cref="System.Exception">will be thrown, if the <see cref="DrawableID"/> or <see cref="LineName"/> don't exists.</exception>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                GameObject drawable = GameDrawableFinder.Find(DrawableID, ParentDrawableID);
                if (drawable == null)
                {
                    throw new System.Exception($"There is no drawable with the ID {DrawableID}.");
                }

                if (Line != null && Line.id != "")
                {
                    GameDrawer.ReDrawLine(drawable, Line);
                }
                else
                {
                    if (OrderInLayer == -1)
                    {
                        GameDrawer.DrawLine(drawable, Name, Positions, Color, Thickness, Loop, LineKind, Tiling);
                    }
                    else
                    {
                        GameDrawer.DrawLine(drawable, Name, Positions, Color, Thickness, OrderInLayer, Loop, LineKind, Tiling);
                    }
                }
            }
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        protected override void ExecuteOnServer()
        {
        }
    }
}