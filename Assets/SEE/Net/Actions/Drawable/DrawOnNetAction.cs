using Assets.SEE.Game;
using Assets.SEE.Game.Drawable;
using SEE.Game;
using SEE.Net.Actions;
using System;
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
        public Vector3 HolderPosition;
        public Vector3 HolderScale;
        public bool Loop;
        public Line Line;

        public DrawOnNetAction(
            string drawableID, string parentDrawableID, string name, Vector3[] positions, Color color, float thickness, int orderInLayer, bool loop)
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
            Loop = loop;
            Line = null;
        }

        public DrawOnNetAction(
            string drawableID, string parentDrawableID, string name, Vector3[] positions, Color color, float thickness, bool loop)
        {
            this.DrawableID = drawableID;
            this.ParentDrawableID = parentDrawableID;
            this.Name = name;
            this.Positions = positions;
            this.Color = color;
            this.Thickness = thickness;
            this.Loop = loop;
            Line = null;
        }
        
        public DrawOnNetAction(string drawableID, string parentDrawableID, Line line)
        {
            this.DrawableID = drawableID;
            this.ParentDrawableID = parentDrawableID;
            this.Line = line;
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

                if (Line != null && Line.id != "")
                {
                    GameDrawer.ReDrawLine(drawable, Line);
                }
                else
                {
                    if (OrderInLayer == -1)
                    {
                        GameDrawer.DrawLine(drawable, Name, Positions, Color, Thickness, Loop);
                    }
                    else
                    {
                        if (Position == Vector3.zero && EulerAngles == Vector3.zero)
                        {
                            GameDrawer.ReDrawRawLine(drawable, Name, Positions, Color, Thickness, OrderInLayer, Loop);
                        } else
                        {
                            GameDrawer.ReDrawLine(drawable, Name, Positions, Color, Thickness, OrderInLayer, Position, EulerAngles, HolderPosition, HolderScale, Loop);
                        }
                    }
                }
            }
        }

        protected override void ExecuteOnServer()
        {
        }
    }
}