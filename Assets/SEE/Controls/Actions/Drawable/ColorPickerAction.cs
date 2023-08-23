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

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Allows to create drawings by the mouse cursor.
    /// It serves as an example for a continuous action that modifies the
    /// scene while active.
    /// </summary>
    class ColorPickerAction : AbstractPlayerAction
    {
        private Color oldChoosenColor;

        private Color pickedColor;

        private Material material;

        private Memento memento;

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if completed</returns>
        public override bool Update()
        {
            bool result = false;

            if (!Raycasting.IsMouseOverGUI())
            {
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) && 
                    Raycasting.RaycastAnything(out RaycastHit raycastHit) &&
                    GameDrawableFinder.hasDrawableParent(raycastHit.collider.gameObject))
                {
                    pickedColor = raycastHit.collider.gameObject.GetColor();
                    HSVPicker.ColorPicker picker = DrawableConfigurator.drawableMenu.GetComponent<HSVPicker.ColorPicker>();
                    picker.AssignColor(DrawableConfigurator.currentColor);
                    picker.onValueChanged.AddListener(DrawableConfigurator.colorAction = color =>
                    {
                        DrawableConfigurator.currentColor = color;
                    });
                    DrawableConfigurator.currentColor = pickedColor;
                    memento = new(oldChoosenColor, pickedColor);
                    result = true;
                    currentState = ReversibleAction.Progress.Completed;
                }/* Materials picker, nicht sinnvoll, da vieles keine Materials aufweisen
                  * else if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) &&
                    Raycasting.RaycastAnything(out RaycastHit raycastHite))
                {
                    material = raycastHite.collider.gameObject.GetComponent<Renderer>().materials[0];
                    Debug.Log("Choosen Material: " + material);
                    DrawableConfigurator.currentMaterial = material;
                }*/
                return Input.GetMouseButtonUp(0);
            }
            return result;
        }

        public override void Awake()
        {
            oldChoosenColor = DrawableConfigurator.currentColor;
            DrawableConfigurator.enableDrawableMenu();
            DrawableConfigurator.disableLayerFromDrawableMenu();
            DrawableConfigurator.disableThicknessFromDrawableMenu();
        }

        public override void Stop()
        {
            DrawableConfigurator.enableLayerFromDrawableMenu();
            DrawableConfigurator.enableThicknessFromDrawableMenu();
            DrawableConfigurator.disableDrawableMenu();
        }

        struct Memento
        {
            public readonly Color oldChoosenColor;
            public readonly Color pickedColor;

            public Memento (Color oldChoosenColor, Color pickedColor)
            {
                this.oldChoosenColor = oldChoosenColor;
                this.pickedColor = pickedColor;
            }
        }

        /// <summary>
        /// Destroys the drawn line.
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Undo()
        {
            base.Undo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            DrawableConfigurator.currentColor = memento.oldChoosenColor;
        }

        /// <summary>
        /// Redraws the drawn line (setting up <see cref="line"/> and adds <see cref="renderer"/> 
        /// before that).
        /// See <see cref="ReversibleAction.Undo()"/>.
        /// </summary>
        public override void Redo()
        {
            base.Redo(); // required to set <see cref="AbstractPlayerAction.hadAnEffect"/> properly.
            DrawableConfigurator.currentColor = memento.pickedColor;
        }

        /// <summary>
        /// A new instance of <see cref="DrawOnAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="DrawOnAction"/></returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new ColorPickerAction();
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
            return ActionStateTypes.ColorPicker;
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
            return new HashSet<string>();
        }
    }
}
