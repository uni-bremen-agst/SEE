using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout
{
    public class CoseNodeLayoutValues
    {
        /// <summary>
        /// the spring force for the x direction
        /// </summary>
        private double springForceX = 0.0;

        /// <summary>
        /// the spring force for the y direction
        /// </summary>
        private double springForceY = 0.0;

        /// <summary>
        /// the repulsion force for the x direction
        /// </summary>
        private double repulsionForceX = 0.0;

        /// <summary>
        /// the repulsion force for the y direction
        /// </summary>
        private double repulsionForceY = 0.0;

        /// <summary>
        /// the gravitation force for the x direction
        /// </summary>
        private double gravitationForceX = 0.0;

        /// <summary>
        /// the gravitation force for the y direction
        /// </summary>
        private double gravitationForceY = 0.0;

        /// <summary>
        /// the displacement for the x direction
        /// </summary>
        private double displacementX = 0.0;

        /// <summary>
        /// the displacement for the y direction
        /// </summary>
        private double displacementY = 0.0;

        /// <summary>
        /// The first grid coorinate for the x axis
        /// </summary>
        private int startX = 0;

        /// <summary>
        /// The first grid coorinate for the y axis
        /// </summary>
        private int startY = 0;

        /// <summary>
        /// The last grid coorinate for the x axis
        /// </summary>
        private int finishX = 0;

        /// <summary>
        /// The last grid coorinate for the y axis
        /// </summary>
        private int finishY = 0;

        /// <summary>
        /// the first predecessor node for the matching (multilevel scaling)
        /// </summary>
        private CoseNode pred1;

        /// <summary>
        /// the second predecessor node for the matching (multilevel scaling)
        /// </summary>
        private CoseNode pred2;

        /// <summary>
        /// The next node for the matching (multilevel scaling)
        /// </summary>
        private CoseNode next;

        /// <summary>
        /// Indicates whether the node was processed in the multilevel scaling process
        /// </summary>
        private bool isProcessed;

        public double SpringForceX { get => springForceX; set => springForceX = value; }
        public double SpringForceY { get => springForceY; set => springForceY = value; }
        public double RepulsionForceX { get => repulsionForceX; set => repulsionForceX = value; }
        public double RepulsionForceY { get => repulsionForceY; set => repulsionForceY = value; }
        public double GravitationForceX { get => gravitationForceX; set => gravitationForceX = value; }
        public double GravitationForceY { get => gravitationForceY; set => gravitationForceY = value; }
        public double DisplacementX { get => displacementX; set => displacementX = value; }
        public double DisplacementY { get => displacementY; set => displacementY = value; }
        public int StartX { get => startX; set => startX = value; }
        public int StartY { get => startY; set => startY = value; }
        public int FinishX { get => finishX; set => finishX = value; }
        public int FinishY { get => finishY; set => finishY = value; }
        public CoseNode Pred1 { get => pred1; set => pred1 = value; }
        public CoseNode Pred2 { get => pred2; set => pred2 = value; }
        public CoseNode Next { get => next; set => next = value; }
        public bool IsProcessed { get => isProcessed; set => isProcessed = value; }
    }
}

