using SEE.DataModel;
using SEE.DataModel.DG;
using SEE.Game;
using SEE.GO;
using SEE.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RootMotion.FinalIK.HitReaction;
using Assets.SEE.Net.Actions.Whiteboard;
using SEE.Net.Actions;
using Assets.SEE.Game.Drawable;
using Assets.SEE.Game;
using Assets.SEE.Net.Actions.Drawable;
using RTG;
using Assets.SEE.Game.UI.Drawable;

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

        /// <summary>
        /// The positions of the line in world space.
        /// </summary>
        private Vector3[] positions = new Vector3[1];

        private Memento memento;

        private const string drawableMenu = "Prefabs/UI/DrawableLineMenu";
        private HSVPicker.ColorPicker picker;
        private ThicknessSliderController thicknessSlider;
        private GameObject menuInstance;

        public override void Start()
        {
            progressState = ProgressState.StartDrawing;
        }

        public override void Awake()
        {
            base.Awake();
            menuInstance = PrefabInstantiator.InstantiatePrefab(drawableMenu,
                            GameObject.Find("UI Canvas").transform, false);

            DrawableLineMenuDestroyer destroyer = menuInstance.AddOrGetComponent<DrawableLineMenuDestroyer>();
            destroyer.SetAllowedState(GetActionStateType());

            thicknessSlider = menuInstance.GetComponentInChildren<ThicknessSliderController>();
            thicknessSlider.AssignValue(DrawableConfigurator.currentThickness);
            thicknessSlider.onValueChanged.AddListener(thickness =>
            {
                DrawableConfigurator.currentThickness = thickness;
            });

            menuInstance.transform.Find("Layer").gameObject.SetActive(false);

            picker = menuInstance.GetComponent<HSVPicker.ColorPicker>();
            picker.AssignColor(DrawableConfigurator.currentColor);
            picker.onValueChanged.AddListener(color =>
            {
                DrawableConfigurator.currentColor = color;
            });
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
                    (raycastHit.collider.gameObject.transform.parent != null && 
                        raycastHit.collider.gameObject.transform.parent.gameObject.CompareTag(Tags.Drawable))))
                {
                    GameObject drawable = raycastHit.collider.gameObject.CompareTag(Tags.Drawable) ? 
                        raycastHit.collider.gameObject : raycastHit.collider.gameObject.transform.parent.gameObject;

                    switch (progressState)
                    {
                        case ProgressState.StartDrawing:
                            progressState = ProgressState.Drawing;
                            positions[0] = raycastHit.point;
                            line = GameDrawer.StartDrawing(drawable, positions, DrawableConfigurator.currentColor, DrawableConfigurator.currentThickness);
                            break;

                        case ProgressState.Drawing:
                            // The position at which to continue the line.
                            Vector3 newPosition = raycastHit.point;

                            // Add newPosition to the line renderer.
                            Vector3[] newPositions = new Vector3[positions.Length + 1];
                            Array.Copy(sourceArray: positions, destinationArray: newPositions, length: positions.Length);
                            newPositions[newPositions.Length - 1] = newPosition;
                            positions = newPositions;

                            if (GameDrawer.DifferentPositionCounter(positions) > 3)
                            {
                                GameDrawer.Drawing(positions);
                                memento = new Memento(drawable, positions, DrawableConfigurator.currentColor,
                                    DrawableConfigurator.currentThickness, line.GetComponent<LineRenderer>().sortingOrder);
                                memento.id = line.name;
                                new DrawOnNetAction(memento.drawable.name, memento.drawable.transform.parent.name,
                                    memento.id, memento.positions, memento.color, memento.thickness).Execute();
                                currentState = ReversibleAction.Progress.InProgress;
                            }
                            break;
                    }
                }
                bool isMouseButtonUp = Input.GetMouseButtonUp(0);
                if (isMouseButtonUp)
                {
                    progressState = ProgressState.FinishDrawing;
                }

                if (progressState == ProgressState.FinishDrawing)
                {
                    if (GameDrawer.DifferentPositionCounter(positions) > 3)
                    {
                        memento.positions = positions;
                        GameDrawer.FinishDrawing();
                        new DrawOnNetAction(memento.drawable.name, memento.drawable.transform.parent.name, memento.id,
                            memento.positions, memento.color, memento.thickness).Execute();
                        result = true;
                        currentState = ReversibleAction.Progress.Completed;
                    } else
                    {
                        Destroyer.Destroy(line);
                    }
                }
                return isMouseButtonUp;
            }
            return result;
        }

        private struct Memento
        {
            public readonly GameObject drawable;

            public Vector3[] positions;

            public readonly Color color;

            public readonly float thickness;

            public readonly int orderInLayer;

            public string id;
            
            public Memento(GameObject drawable, Vector3[] positions, Color color, float thickness, int orderInLayer)
            {
                this.drawable = drawable;
                this.positions = positions;
                this.color = color;
                this.thickness = thickness;
                this.orderInLayer = orderInLayer;
                this.id = null;
            }
        }

        /// <summary>
        /// Destroys the drawn line.
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            if (line == null)
            {
                line = GameDrawableIDFinder.FindChild(memento.drawable, memento.id);
            }
            new LineEraseNetAction(memento.drawable.name, memento.drawable.transform.parent.name, memento.id).Execute();
            Destroyer.Destroy(line);
            line = null;
        }

        /// <summary>
        /// Redraws the drawn line (setting up <see cref="line"/> and adds <see cref="renderer"/> 
        /// before that).
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Redo()
        {
            base.Redo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            line = GameDrawer.ReDrawLine(memento.drawable, memento.id, memento.positions, memento.color, 
                memento.thickness, memento.orderInLayer);
            if (line != null)
            {
                new DrawOnNetAction(memento.drawable.name, memento.drawable.transform.parent.name, 
                    memento.id, memento.positions, memento.color, 
                    memento.thickness, memento.orderInLayer).Execute();
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
                    memento.id
                };
            }
        }
    }
}
