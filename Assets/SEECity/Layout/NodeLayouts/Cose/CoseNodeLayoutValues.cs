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
        private float springForceX = 0.0f;

        /// <summary>
        /// the spring force for the y direction
        /// </summary>
        private float springForceY = 0.0f;

        /// <summary>
        /// the repulsion force for the x direction
        /// </summary>
        private float repulsionForceX = 0.0f;

        /// <summary>
        /// the repulsion force for the y direction
        /// </summary>
        private float repulsionForceY = 0.0f;

        /// <summary>
        /// the gravitation force for the x direction
        /// </summary>
        private float gravitationForceX = 0.0f;

        /// <summary>
        /// the gravitation force for the y direction
        /// </summary>
        private float gravitationForceY = 0.0f;

        /// <summary>
        /// the displacement for the x direction
        /// </summary>
        private float displacementX = 0.0f;

        /// <summary>
        /// the displacement for the y direction
        /// </summary>
        private float displacementY = 0.0f;

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

        public float SpringForceX { get => springForceX; set => springForceX = value; }
        public float SpringForceY { get => springForceY; set => springForceY = value; }
        public float RepulsionForceX { get => repulsionForceX; set => repulsionForceX = value; }
        public float RepulsionForceY { get => repulsionForceY; set => repulsionForceY = value; }
        public float GravitationForceX { get => gravitationForceX; set => gravitationForceX = value; }
        public float GravitationForceY { get => gravitationForceY; set => gravitationForceY = value; }
        public float DisplacementX { get => displacementX; set => displacementX = value; }
        public float DisplacementY { get => displacementY; set => displacementY = value; }
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

