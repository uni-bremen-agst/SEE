using SEE.Game;
using SEE.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using SEE.Net.Actions.Drawable;
using Assets.SEE.Game.Drawable;
using Assets.SEE.Game;
using System.Linq;
using SEE.Controls.Actions;
using Assets.SEE.Game.UI.Drawable;
using SEE.Game.Drawable.Configurations;

namespace SEE.Controls.Actions.Drawable
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
        private bool finishDrawing = false;

        public override void Start()
        {
            progressState = ProgressState.StartDrawing;
        }

        public override void Awake()
        {
            LineMenu.enableLineMenu(withoutMenuLayer: new LineMenu.MenuLayer[] { LineMenu.MenuLayer.Layer, LineMenu.MenuLayer.Loop });

            LineMenu.GetTilingSliderController().onValueChanged.AddListener(LineMenu.tilingAction = tiling =>
            {
                ValueHolder.currentTiling = tiling;
            });
            LineMenu.GetNextLineKindBtn().onClick.RemoveAllListeners();
            LineMenu.GetNextLineKindBtn().onClick.AddListener(() => ValueHolder.currentLineKind = LineMenu.NextLineKind());
            LineMenu.GetPreviousLineKindBtn().onClick.RemoveAllListeners();
            LineMenu.GetPreviousLineKindBtn().onClick.AddListener(() => ValueHolder.currentLineKind = LineMenu.PreviousLineKind());

            thicknessSlider = LineMenu.instance.GetComponentInChildren<ThicknessSliderController>();
            thicknessSlider.AssignValue(ValueHolder.currentThickness);
            thicknessSlider.onValueChanged.AddListener(thickness =>
            {
                ValueHolder.currentThickness = thickness;
            });

            picker = LineMenu.instance.GetComponent<HSVPicker.ColorPicker>();
            picker.AssignColor(ValueHolder.currentColor);
            picker.onValueChanged.AddListener(LineMenu.colorAction = color =>
            {
                ValueHolder.currentColor = color;
            });
        }

        public override void Stop()
        {
            LineMenu.disableLineMenu();
        }

        /// <summary>
        /// Continues the line at the point of the mouse position and draws it.
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && !finishDrawing &&
                    Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                    (raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ||
                    GameDrawableFinder.hasDrawable(raycastHit.collider.gameObject))
                    && (drawable == null || drawable != null && GameDrawableFinder.FindDrawable(raycastHit.collider.gameObject).Equals(drawable)))
                {
                    switch (progressState)
                    {
                        case ProgressState.StartDrawing:
                            drawable = raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ?
                                    raycastHit.collider.gameObject : GameDrawableFinder.FindDrawable(raycastHit.collider.gameObject);
                            drawing = true;
                            progressState = ProgressState.Drawing;
                            positions[0] = raycastHit.point;
                            line = GameDrawer.StartDrawing(drawable, positions, ValueHolder.currentColor, ValueHolder.currentThickness,
                                ValueHolder.currentLineKind, ValueHolder.currentTiling);
                            positions[0] = line.transform.InverseTransformPoint(positions[0]) - ValueHolder.distanceToDrawable;
                            break;

                        case ProgressState.Drawing:
                            // The position at which to continue the line.
                            Vector3 newPosition = line.transform.InverseTransformPoint(raycastHit.point) - ValueHolder.distanceToDrawable;
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

                if (Input.GetMouseButtonUp(0) && drawing)
                {
                    progressState = ProgressState.FinishDrawing;
                    drawing = false;

                    if (progressState == ProgressState.FinishDrawing)
                    {
                        if (GameDrawer.DifferentMeshVerticesCounter(line) >= 3)
                        {
                            finishDrawing = true;
                            line = GameDrawer.SetPivot(line);
                            Line currentLine = Line.GetLine(line);
                            memento = new Memento(drawable, currentLine);
                            new DrawOnNetAction(memento.drawable.name, GameDrawableFinder.GetDrawableParentName(memento.drawable), currentLine).Execute();
                            currentState = ReversibleAction.Progress.Completed;
                            return true; 
                        }
                        else
                        {
                            Destroyer.Destroy(line);
                            progressState = ProgressState.StartDrawing;
                            positions = new Vector3[1];
                        }
                    }
                    return false;
                }
            }
            return false;
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
            base.Undo();
            if (line == null)
            {
                line = GameDrawableFinder.FindChild(memento.drawable, memento.line.id);
            }
            if (line != null)
            {
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