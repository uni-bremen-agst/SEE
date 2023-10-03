using SEE.Game;
using SEE.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.SEE.Net.Actions.Whiteboard;
using SEE.Net.Actions;
using Assets.SEE.Game.Drawable;
using Assets.SEE.Game;
using System.Linq;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Allows to create drawings by the mouse cursor.
    /// It serves as an example for a continuous action that modifies the
    /// scene while active.
    /// </summary>
    class DrawOnAction : AbstractPlayerAction
    {
        private enum ProgressState
        {
            StartDrawing,
            Drawing,
            FinishDrawing
        }

        private ProgressState progressState;

        /// <summary>
        /// The object holding the line renderer.
        /// </summary>
        private GameObject line;

        private GameObject drawable;

        /// <summary>
        /// The positions of the line in local space.
        /// </summary>
        private Vector3[] positions = new Vector3[1];

        private Memento memento;

        private HSVPicker.ColorPicker picker;
        private ThicknessSliderController thicknessSlider;
       private bool drawing = false;

        public override void Start()
        {
            progressState = ProgressState.StartDrawing;
        }

        public override void Awake()
        {
            DrawableHelper.enableDrawableMenu(withoutMenuPoints: new DrawableHelper.MenuPoint[] { DrawableHelper.MenuPoint.Layer, DrawableHelper.MenuPoint.Loop });

            DrawableHelper.GetTilingSlider().onValueChanged.AddListener(DrawableHelper.tilingAction = tiling =>
            {
                DrawableHelper.currentTiling = tiling;
            });
            DrawableHelper.GetNextBtn().onClick.RemoveAllListeners();
            DrawableHelper.GetNextBtn().onClick.AddListener(() => DrawableHelper.currentLineKind = DrawableHelper.NextLineKind());
            DrawableHelper.GetPreviousBtn().onClick.RemoveAllListeners();
            DrawableHelper.GetPreviousBtn().onClick.AddListener(() => DrawableHelper.currentLineKind = DrawableHelper.PreviousLineKind());

            thicknessSlider = DrawableHelper.drawableMenu.GetComponentInChildren<ThicknessSliderController>();
            thicknessSlider.AssignValue(DrawableHelper.currentThickness);
            thicknessSlider.onValueChanged.AddListener(thickness =>
            {
                DrawableHelper.currentThickness = thickness;
            });

            picker = DrawableHelper.drawableMenu.GetComponent<HSVPicker.ColorPicker>();
            picker.AssignColor(DrawableHelper.currentColor);
            picker.onValueChanged.AddListener(DrawableHelper.colorAction = color =>
            {
                DrawableHelper.currentColor = color;
            });
        }

        public override void Stop()
        {
            DrawableHelper.disableDrawableMenu();
        }

        /// <summary>
        /// Continues the line at the point of the mouse position and draws it.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;

            if (!Raycasting.IsMouseOverGUI())
            {
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) &&
                    Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                    (raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ||
                    GameDrawableFinder.hasDrawableParent(raycastHit.collider.gameObject)))
                {
                    drawable = raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ?
                        raycastHit.collider.gameObject : GameDrawableFinder.FindDrawableParent(raycastHit.collider.gameObject);
                    drawing = true;

                    switch (progressState)
                    {
                        case ProgressState.StartDrawing:
                            progressState = ProgressState.Drawing;
                            positions[0] = raycastHit.point;
                            line = GameDrawer.StartDrawing(drawable, positions, DrawableHelper.currentColor, DrawableHelper.currentThickness, 
                                DrawableHelper.currentLineKind, DrawableHelper.currentTiling);
                            positions[0] = line.transform.InverseTransformPoint(positions[0]) - DrawableHelper.distanceToBoard;
                            break;

                        case ProgressState.Drawing:
                            // The position at which to continue the line.
                            Vector3 newPosition = line.transform.InverseTransformPoint(raycastHit.point) - DrawableHelper.distanceToBoard;
                            if (newPosition != positions.Last())
                            {
                                // Add newPosition to the line renderer.
                                Vector3[] newPositions = new Vector3[positions.Length + 1];
                                Array.Copy(sourceArray: positions, destinationArray: newPositions, length: positions.Length);
                                newPositions[newPositions.Length - 1] = newPosition;
                                positions = newPositions;

                                GameDrawer.Drawing(line, positions);
                                new DrawOnNetAction(drawable.name, GameDrawableFinder.GetDrawableParentName(drawable), Line.GetLine(line)).Execute();
                                currentState = ReversibleAction.Progress.InProgress;
                            }
                            break;
                    }
                }

                bool isMouseButtonUp = Input.GetMouseButtonUp(0);
                if (isMouseButtonUp || (!Input.GetMouseButton(0) && drawing))
                {
                    progressState = ProgressState.FinishDrawing;
                    drawing = false;

                    if (progressState == ProgressState.FinishDrawing)
                    {
                        if (GameDrawer.DifferentMeshVerticesCounter(line) >= 3)
                        {
                            line = GameDrawer.SetPivot(line);
                            Line currentLine = Line.GetLine(line);
                            memento = new Memento(drawable, currentLine);
                            new DrawOnNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), currentLine).Execute();
                            Debug.Log("Created: " + line);
                            result = true;
                            currentState = ReversibleAction.Progress.Completed;
                            progressState = ProgressState.StartDrawing;
                            positions = new Vector3[1];
                        }
                        else
                        {
                            Destroyer.Destroy(line);//.transform.parent.gameObject);
                        }
                    }
                    return isMouseButtonUp;
                }
            }
            return result;
        }

        private struct Memento
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
            Debug.Log("Undo: " + line + ", id:" + memento.line.id);
            if (line == null)
            {
                line = GameDrawableFinder.FindChild(memento.drawable, memento.line.id);
                Debug.Log("Try to find: " + line);
            }
            if (line != null)
            {
                Debug.Log("Delete: " + line);
                new EraseNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), memento.line.id).Execute();
                Destroyer.Destroy(line);//.transform.parent.gameObject);
                line = null;
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
            line = GameDrawer.ReDrawLine(memento.drawable, memento.line);
            if (line != null)
            {
                new DrawOnNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), Line.GetLine(line)).Execute();
            }
        }

        /// <summary>
        /// A new instance of <see cref="DrawOnAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DrawOnAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new DrawOnAction();
        }

        /// <summary>
        /// A new instance of <see cref="DrawOnAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DrawOnAction"/></returns>
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
            return ActionStateTypes.DrawOn;
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
            if (memento.drawable == null)
            {
                return new HashSet<string>();
            }
            else
            {
                return new HashSet<string>
                {
                    memento.drawable.name,
                    memento.line.id
                };
            }
        }
    }
}