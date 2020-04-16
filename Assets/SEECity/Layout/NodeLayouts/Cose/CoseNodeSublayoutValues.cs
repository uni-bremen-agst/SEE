using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.DataModel;
using SEE;

namespace SEE.Layout
{
    public class CoseNodeSublayoutValues
    {
        /// <summary>
        /// the bounds of the node relative to its sublayout root 
        /// </summary>
        //public Rect relativeRect = new Rect(0, 0, 0, 0);

        /// TODO, raucht man glab ich nicht, außer vielleicht für das evostreet?
        private Vector3 relativeScale;

        /// TODO
        private Vector3 relativeCenterPosition;

        /// <summary>
        /// Indicates whether the node is a sublayout root
        /// </summary>
        private bool isSubLayoutRoot = false;

        /// <summary>
        /// Indicates whether the node is a sublayout node
        /// </summary>
        private bool isSubLayoutNode = false;

        /// <summary>
        /// The root node of the sublayout the node is part of 
        /// </summary>
        private CoseNode subLayoutRoot = null;

        /// <summary>
        /// 
        /// </summary>
        private Sublayout sublayout;

        public bool IsSubLayoutRoot { get => isSubLayoutRoot; set => isSubLayoutRoot = value; }
        public bool IsSubLayoutNode { get => isSubLayoutNode; set => isSubLayoutNode = value; }
        public CoseNode SubLayoutRoot { get => subLayoutRoot; set => subLayoutRoot = value; }
        public Sublayout Sublayout { get => sublayout; set => sublayout = value; }
        public Vector3 RelativeScale { get => relativeScale; set => relativeScale = value; }
        public Vector3 RelativeCenterPosition { get => relativeCenterPosition; set => relativeCenterPosition = value; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        public void SetLocationRelative(float x, float z)
        {
            relativeCenterPosition.x = x;
            relativeCenterPosition.z = z;
        }

        /// <summary>
        /// updates the relative bounding rect
        /// </summary>
        /// <param name="left">the left position</param>
        /// <param name="right">the right position</param>
        /// <param name="top">the top position</param>
        /// <param name="bottom">the bottom position</param>
        public void UpdateRelativeBounding(Vector3 scale, Vector3 position)
        {
            relativeScale = scale;
            relativeCenterPosition = position;
        }
    }
}

