using System;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Action to create an edge between two selected nodes.
    /// </summary>
    public class AddEdgeAction : AbstractPlayerAction
    {
        /// <summary>
        /// Returns a new instance of <see cref="AddEdgeAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="AddEdgeAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new AddEdgeAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="AddEdgeAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="AddEdgeAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// The source for an edge to be drawn.
        /// </summary>
        private GameObject from;

        /// <summary>
        /// The target of the edge to be drawn.
        /// </summary>
        private GameObject to;

        /// <summary>
        /// The Objects which are needed to create a new edge:
        /// The source, the target and the city where the edge will be attached to.
        /// </summary>
        private Tuple<GameObject, GameObject, SEECity> edgeToBeDrawn;

        /// <summary>
        /// The edge created by this action.
        /// </summary>
        private GameObject createdEdge;

        /// <summary>
        /// The name of the generated edge.
        /// </summary>
        private string edgeName;

        public override void Start()
        {
            InteractableObject.LocalAnyHoverIn += LocalAnyHoverIn;
            InteractableObject.LocalAnyHoverOut += LocalAnyHoverOut;
        }

        /// <summary>
        /// <see cref="ReversibleAction.Update"/>.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;

            // Assigning the game objects to be connected.
            // Checking whether the two game objects are not null and whether they are 
            // actually nodes.
            if (Input.GetMouseButtonDown(0) && hoveredObject != null)
            {
                Assert.IsTrue(hoveredObject.HasNodeRef());
                if (from == null)
                {
                    from = hoveredObject;
                }
                else if (to == null)
                {
                    to = hoveredObject;
                }
            }
            // Note: from == to may be possible.
            if (from != null && to != null)
            {
                Transform cityObject = SceneQueries.GetCodeCity(from.transform);
                if (cityObject != null)
                {
                    if (cityObject.TryGetComponent(out SEECity city))
                    {
                        try
                        {
                            GameObject addedEdge = city.Renderer.DrawEdge(from, to, null);
                            edgeToBeDrawn = new Tuple<GameObject, GameObject, SEECity>(from, to, city);
                            createdEdge = addedEdge;
                            new AddEdgeNetAction(from.name, to.name).Execute();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"The new edge from {from.name} to {to.name} could not be created: {e.Message}.\n");
                        }
                        from = null;
                        to = null;
                        // action is completed (successfully or not; it does not matter)
                        result = true;
                        hadAnEffect = true;
                    }
                }
            }
            // Adding the key "F1" in order to forget the selected GameObjects.
            if (Input.GetKeyDown(KeyCode.F1))
            {
                from = null;
                to = null;
            }
            return result;
        }

        /// <summary>
        /// Undoes this AddEdgeAction
        /// </summary>
        public override void Undo()
        {
            base.Undo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            DeleteAction deleteAction = new DeleteAction();
            deleteAction.DeleteSelectedObject(createdEdge);
            edgeName = createdEdge.name;
            Destroyer.DestroyGameObject(createdEdge);
        }

        /// <summary>
        /// Redoes this AddEdgeAction
        /// </summary>
        public override void Redo()
        {
            base.Redo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            GameObject redoneEdge = edgeToBeDrawn.Item3.Renderer.DrawEdge(edgeToBeDrawn.Item1, edgeToBeDrawn.Item2, edgeName);
            createdEdge = redoneEdge;
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns>the <see cref="ActionStateType"/> of this action</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.NewEdge;
        }
    }
}