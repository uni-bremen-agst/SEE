using SEE.DataModel.DG;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{

    /// <summary>
    /// Saves the relevant parameter of an action for saving an instance for the possibility of an undo. 
    /// </summary>
    public class UndoAction
    {
        /// <summary>
        /// A history of all deleted nodes.
        /// </summary>
        private List<GameObject> deletedNodes;

        /// <summary>
        /// A history of the old positions of the deleted nodes.
        /// </summary>
        private List<Vector3> oldPositions;

        /// <summary>
        /// A history of all deleted edges.
        /// </summary>
        private List<GameObject> deletedEdges;

        /// <summary>
        /// The graph where the action is executed.
        /// </summary>
        private Graph graph;

        /// <summary>
        /// Creates an instance of the undoAction
        /// </summary>
        /// <param name="deletedNodes">the deletedNodes in the last action if exiting</param>
        /// <param name="oldPositions">the old positions of the deleted nodes if existing</param>
        /// <param name="deletedEdges">the deletedEdges in the last action if existing</param>
        /// <param name="graph">the graph where the action is executed</param>
        public UndoAction(List<GameObject> deletedNodes, List<Vector3> oldPositions, List<GameObject> deletedEdges, Graph graph)
        {
            this.DeletedNodes = deletedNodes;
            this.OldPositions = oldPositions;
            this.DeletedEdges = deletedEdges;
            this.Graph = graph;
        }

        public List<GameObject> DeletedEdges { get => deletedEdges; set => deletedEdges = value; }
        public List<Vector3> OldPositions { get => oldPositions; set => oldPositions = value; }
        public List<GameObject> DeletedNodes { get => deletedNodes; set => deletedNodes = value; }
        public Graph Graph { get => graph; set => graph = value; }
    }
}
 