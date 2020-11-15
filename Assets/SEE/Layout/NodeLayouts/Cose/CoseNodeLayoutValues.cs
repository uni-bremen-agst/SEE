// Copyright 2020 Nina Unterberg
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO
// EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR
// THE USE OR OTHER DEALINGS IN THE SOFTWARE.

namespace SEE.Layout.NodeLayouts.Cose
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

