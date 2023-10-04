using Assets.SEE.Game;
using Assets.SEE.Game.Drawable;
using SEE.Net.Actions.Drawable;
using SEE.Game;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// Allows to create drawings by the mouse cursor.
    /// It serves as an example for a continuous action that modifies the
    /// scene while active.
    /// </summary>
    class EraseAction : AbstractPlayerAction
    {


        /// <summary>
        /// Continues the line at the point of the mouse position and draws it.
        /// See <see cref="ReversibleAction.Awake"/>.
        /// </summary>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) &&
                    Raycasting.RaycastAnythingBackface(out RaycastHit raycastHit) &&
                    GameDrawableFinder.hasDrawable(raycastHit.collider.gameObject))
                {
                    GameObject hittedObject = raycastHit.collider.gameObject;
                    if (hittedObject.CompareTag(Tags.Line))
                    {
                        LineRenderer lineRenderer = hittedObject.GetComponent<LineRenderer>();
                        Vector3[] positions = new Vector3[lineRenderer.positionCount];
                        lineRenderer.GetPositions(positions);
                        memento = new Memento(GameDrawableFinder.FindDrawable(hittedObject), Line.GetLine(hittedObject));
                        mementoList.Add(memento);

                        new EraseNetAction(memento.drawable.name, memento.drawable.transform.parent.name, memento.line.id).Execute();
                        Destroyer.Destroy(hittedObject);
                       // Destroyer.Destroy(hittedObject.transform.parent.gameObject);
                    }
                }
                bool isMouseButtonUp = Input.GetMouseButtonUp(0);
                // The action is considered complete if the mouse button is no longer pressed.
                if (isMouseButtonUp)
                {
                    currentState = ReversibleAction.Progress.Completed;
                }
                // The action is considered complete if the mouse button is no longer pressed.
                return isMouseButtonUp;
            }
            return false;
        }

        private List<Memento> mementoList = new List<Memento>();
        private Memento memento;

        private class Memento
        {
            public readonly GameObject drawable;
            public Line line;

            public Memento(GameObject drawable, Line line)
            {
                this.drawable = drawable;
                this.line = line;
            }
        }

        /// <summary>
        /// Destroys the drawn line.
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            foreach (Memento mem in mementoList)
            {
                mem.line.gameObject = GameDrawer.ReDrawLine(mem.drawable, mem.line);
                new DrawOnNetAction(mem.drawable.name, GameDrawableFinder.GetDrawableParentName(mem.drawable), mem.line).Execute();
            }
        }

        /// <summary>
        /// Redraws the drawn line (setting up <see cref="line"/> and adds <see cref="renderer"/> 
        /// before that).
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Redo()
        {
            base.Redo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            foreach (Memento mem in mementoList)
            {
                if (mem.line.gameObject == null && mem.line.id != null)
                {
                    mem.line.gameObject = GameDrawableFinder.FindChild(mem.drawable, mem.line.id);
                }
                new EraseNetAction(mem.drawable.name, GameDrawableFinder.GetDrawableParentName(mem.drawable), mem.line.id).Execute();
                Destroyer.Destroy(mem.line.gameObject);
              //  Destroyer.Destroy(mem.line.gameObject.transform.parent.gameObject);
            }
        }

        /// <summary>
        /// A new instance of <see cref="EraseAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EraseAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new EraseAction();
        }

        /// <summary>
        /// A new instance of <see cref="EraseAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EraseAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.DrawOnWhiteboard"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.Erase;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// Because this action does not actually change any game object, 
        /// an empty set is always returned.
        /// </summary>
        /// <returns>an empty set</returns>
        public override HashSet<string> GetChangedObjects()
        {
            if (memento == null || memento.drawable == null)
            {
                return new HashSet<string>();
            }
            else
            {
                HashSet<string> changedObjects = new HashSet<string>();
                changedObjects.Add(memento.drawable.name);
                foreach (Memento mem in mementoList)
                {
                    changedObjects.Add(mem.line.id);
                }
                return changedObjects;
            }
        }
    }
}
