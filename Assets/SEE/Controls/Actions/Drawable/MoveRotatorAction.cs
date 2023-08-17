using Assets.SEE.Game;
using Assets.SEE.Game.Drawable;
using Assets.SEE.Game.UI.Drawable;
using Assets.SEE.Net.Actions.Drawable;
using Assets.SEE.Net.Actions.Whiteboard;
using RTG;
using SEE.Controls.Actions;
using SEE.Game;
using SEE.Game.UI.ConfigMenu;
using SEE.GO;
using SEE.Net.Actions;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UIElements;
using MoveNetAction = Assets.SEE.Net.Actions.Drawable.MoveNetAction;

namespace Assets.SEE.Controls.Actions.Drawable
{
    public class MoveRotatorAction : AbstractPlayerAction
    {
        private Memento memento;
        private bool didSomething = false;
        private bool isDone = false;
        private GameObject selectedObject;

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;
            selectedObject = GameMoveRotator.selectedObj;

            if (!Raycasting.IsMouseOverGUI())
            {
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && !GameMoveRotator.isActive && !didSomething && !isDone &&
                    Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                    raycastHit.collider.gameObject.transform.parent.gameObject.CompareTag(Tags.Drawable))
                {
                    selectedObject = raycastHit.collider.gameObject;
                    GameMoveRotator.isActive = true;
                    BlinkEffect effect = selectedObject.AddOrGetComponent<BlinkEffect>();
                    effect.SetAllowedActionStateType(GetActionStateType());
                    effect.Activate(selectedObject);
                    GameMoveRotator.SetSelectedLine(selectedObject);
                    GameMoveRotator.firstPoint = raycastHit.point;
                }

                if (selectedObject != null && selectedObject.GetComponent<BlinkEffect>() != null && selectedObject.GetComponent<BlinkEffect>().GetLoopStatus())
                {   // MOVE
                    if (Raycasting.RaycastAnything(out RaycastHit hit))
                    {
                        if (hit.collider.gameObject.CompareTag(Tags.Drawable))
                        {
                            didSomething = true;
                            Vector3 linePosition = GameMoveRotator.oldObjectPosition;
                            GameMoveRotator.newObjectPosition = linePosition + new Vector3(hit.point.x - GameMoveRotator.firstPoint.x, hit.point.y - GameMoveRotator.firstPoint.y, 0);
                            GameMoveRotator.MoveObject(selectedObject, GameMoveRotator.newObjectPosition);
                            GameObject drawable = selectedObject.transform.parent.gameObject;
                            GameObject drawableParent = drawable.transform.parent.gameObject;
                            new MoveNetAction(drawable.name, drawableParent.name, selectedObject.name, GameMoveRotator.newObjectPosition).Execute();
                        }
                    }
                    // Rotate ?
                    /*
                    LineRenderer renderer = selectedObject.GetComponent<LineRenderer>();
                    Vector3[] positions = GameMoveRotator.oldLinePositions;
                    if (Input.mouseScrollDelta.y > 0)
                    {
                        Debug.Log(DateTime.Now + " Mouse UP! " + Input.mouseScrollDelta.y);
                        if (selectedObject.CompareTag(Tags.Line))
                        {
                            // Tricky shit
                            Debug.Log("Positionsize: " + positions.Length + " - Render size: " + renderer.positionCount);
                            for (int i = 0; i < renderer.positionCount; i++)
                            {

                                Vector3 position = positions[i];
                                //float offsetX = hit.point.x - position.x;
                                //float offsetY = hit.point.y - position.y;
                                Vector3 moveOffset = position;
                                Debug.Log("Step: " + GameMoveRotator.step);
                                Debug.Log("First line: " + positions[0]);
                                switch (GameMoveRotator.step)
                                {
                                    case 0:
                                        moveOffset = new(0, position.y - GameMoveRotator.firstPoint.y, 0);
                                        break;
                                    case 1:
                                        moveOffset = new(GameMoveRotator.firstPoint.x - position.x, 0, 0);
                                        break;
                                    case 2:
                                        moveOffset = new(GameMoveRotator.firstPoint.x - position.x, GameMoveRotator.firstPoint.y - position.y, 0);
                                        break;
                                    case 3:
                                        moveOffset = new(0, 0, 0);
                                        break;
                                    default:
                                        GameMoveRotator.step = 0;
                                        break;
                                }

                                positions[i] = position + moveOffset;
                            }
                            GameMoveRotator.step++;
                            if (GameMoveRotator.step >= 4)
                            {
                                GameMoveRotator.step = 0;
                            }
                            renderer.SetPositions(positions);
                            Mesh mesh = new();
                            MeshCollider meshCollider = selectedObject.GetComponent<MeshCollider>();
                            renderer.BakeMesh(mesh, false);
                            meshCollider.sharedMesh = mesh;
                        } else
                        {
                            // for image etc ?
                        }
                    }
                    if (Input.mouseScrollDelta.y < 0)
                    {
                        Debug.Log(DateTime.Now + " Mouse Down! " + Input.mouseScrollDelta.y);
                        if (selectedObject.CompareTag(Tags.Line))
                        {
                            // Tricky shit
                        }
                        else
                        {
                            // for image etc ?
                        }
                    }
                    /*
                             LineRenderer renderer = selectedLine.GetComponent<LineRenderer>();
                             Vector3[] positions = GameMoveRotator.savedPositionsOfselectedLine;

                            renderer.GetPositions(positions);
                             Debug.Log("Positionsize: " + positions.Length + " - Render size: " + renderer.positionCount);
                                for (int i = 0; i < renderer.positionCount; i++)
                                {
                                    
                                    Vector3 position = positions[i];
                                    float offsetX = hit.point.x - position.x;
                                    float offsetY = hit.point.y - position.y;

                                    Vector3 moveOffset = new(offsetX, offsetY, 0);

                                positions[i] = hit.point + moveOffset;
                                }
                                renderer.SetPositions(positions);
                                Mesh mesh = new();
                                MeshCollider meshCollider = selectedLine.GetComponent<MeshCollider>();
                                //Destroyer.Destroy(meshCollider);
                               // MeshCollider newMeshCollider = selectedLine.AddComponent<MeshCollider>(); 
                                renderer.BakeMesh(mesh, false);
                                meshCollider.sharedMesh = mesh;
                            */
                    //Debug.Log("Rotate");
                    //Vector3 newRotation = new Vector3(0, 0, 0);
                    //selectedLine.transform.localEulerAngles = newRotation;
                }
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && GameMoveRotator.selectedObj != null && didSomething && GameMoveRotator.isActive)
                {
                    memento = new Memento(selectedObject, selectedObject.transform.parent.gameObject, selectedObject.name,
                        GameMoveRotator.oldObjectPosition, GameMoveRotator.newObjectPosition, GameMoveRotator.oldLinePositions, GameMoveRotator.newLinePositions);
                    GameMoveRotator.isActive = false;
                    isDone = true;
                    didSomething = false;
                    selectedObject.GetComponent<BlinkEffect>().Deactivate();
                    selectedObject = null;
                    GameMoveRotator.selectedObj = null;
                    GameMoveRotator.oldObjectPosition = new Vector3();
                    result = true;
                    currentState = ReversibleAction.Progress.Completed;
                }
                return Input.GetMouseButtonUp(0);
            }
            return result;
        }

        private struct Memento
        {
            public GameObject selectedLine;
            public readonly GameObject drawable;
            public readonly string currentLineName;
            public readonly Vector3 oldObjectPosition;
            public readonly Vector3 newObjectPosition;
            public readonly Vector3[] oldLinePositions;
            public readonly Vector3[] newLinePositions;

            public Memento(GameObject selectedLine, GameObject drawable, string currentLineName,
                Vector3 oldObjectPosition, Vector3 newObjectPosition, Vector3[] oldLinePositions, Vector3[] newLinePositions)
            {
                this.selectedLine = selectedLine;
                this.drawable = drawable;
                this.currentLineName = currentLineName;
                this.oldObjectPosition = oldObjectPosition;
                this.newObjectPosition = newObjectPosition;
                this.oldLinePositions = oldLinePositions;
                this.newLinePositions = newLinePositions;
            }
        }

        /// <summary>
        /// Destroys the drawn line.
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            if (memento.selectedLine == null && memento.currentLineName != null)
            {
                memento.selectedLine = GameDrawableIDFinder.FindChild(memento.drawable, memento.currentLineName);
            }

            if (memento.selectedLine != null)
            {
                GameMoveRotator.MoveObject(memento.selectedLine, memento.oldObjectPosition);
                GameMoveRotator.RotateLine(memento.selectedLine, memento.oldLinePositions);
                GameObject drawable = memento.selectedLine.transform.parent.gameObject;
                GameObject drawableParent = drawable.transform.parent.gameObject;
                new MoveNetAction(drawable.name, drawableParent.name, memento.currentLineName, memento.oldObjectPosition).Execute();
                new RotatorNetAction(drawable.name, drawableParent.name, memento.currentLineName, memento.oldLinePositions).Execute();
            }
            if (memento.selectedLine != null && memento.selectedLine.TryGetComponent<BlinkEffect>(out BlinkEffect currentEffect))
            {
                currentEffect.Deactivate();
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
            if (memento.selectedLine == null && memento.currentLineName != null)
            {
                memento.selectedLine = GameDrawableIDFinder.FindChild(memento.drawable, memento.currentLineName);
            }
            if (memento.selectedLine != null)
            {
                GameMoveRotator.MoveObject(memento.selectedLine, memento.newObjectPosition);
                GameMoveRotator.RotateLine(memento.selectedLine, memento.newLinePositions);
                GameObject drawable = memento.selectedLine.transform.parent.gameObject;
                GameObject drawableParent = drawable.transform.parent.gameObject;
                new MoveNetAction(drawable.name, drawableParent.name, memento.currentLineName, memento.newObjectPosition).Execute();
                new RotatorNetAction(drawable.name, drawableParent.name, memento.currentLineName, memento.newLinePositions).Execute();
            }

            if (memento.selectedLine != null && memento.selectedLine.TryGetComponent<BlinkEffect>(out BlinkEffect currentEffect))
            {
                currentEffect.Deactivate();
            }
        }

        /// <summary>
        /// A new instance of <see cref="EditLineAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EditLineAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new MoveRotatorAction();
        }

        /// <summary>
        /// A new instance of <see cref="EditLineAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="EditLineAction"/></returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.MoveRotator;
        }

        public override HashSet<string> GetChangedObjects()
        {
            if (memento.selectedLine == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return new HashSet<string>
                {
                    memento.selectedLine.name
                };
            }
        }
    }
}