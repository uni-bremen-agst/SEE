using SEE.GO;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using Assets.SEE.Game.Drawable;
using Assets.SEE.Game;
using Assets.SEE.Game.UI.Drawable;

namespace SEE.Controls.Actions.Drawable
{
    class ColorPickerAction : AbstractPlayerAction
    {
        private Color oldChosenColor;

        private Color pickedColor;

        private Memento memento;

        private HSVPicker.ColorPicker picker;


        public override bool Update()
        {
            bool result = false;

            if (!Raycasting.IsMouseOverGUI())
            {
                if ((Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) &&
                    Raycasting.RaycastAnythingBackface(out RaycastHit raycastHit) &&
                    GameDrawableFinder.hasDrawable(raycastHit.collider.gameObject))
                {
                    pickedColor = raycastHit.collider.gameObject.GetColor();
                    picker.AssignColor(pickedColor);
                    ValueHolder.currentColor = pickedColor;

                    memento = new(oldChosenColor, pickedColor);
                    result = true;
                    currentState = ReversibleAction.Progress.Completed;
                }
            }
            return result;
        }

        public override void Awake()
        {
            oldChosenColor = ValueHolder.currentColor;
            LineMenu.enableLineMenu(withoutMenuLayer: new LineMenu.MenuLayer[] { LineMenu.MenuLayer.All});
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

        struct Memento
        {
            public readonly Color oldChosenColor;
            public readonly Color pickedColor;

            public Memento (Color oldChosenColor, Color pickedColor)
            {
                this.oldChosenColor = oldChosenColor;
                this.pickedColor = pickedColor;
            }
        }

        public override void Undo()
        {
            base.Undo();
            ValueHolder.currentColor = memento.oldChosenColor;
            picker.AssignColor (memento.oldChosenColor);
        }


        public override void Redo()
        {
            base.Redo();
            ValueHolder.currentColor = memento.pickedColor;
        }


        public static ReversibleAction CreateReversibleAction()
        {
            return new ColorPickerAction();
        }


        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.ColorPicker;
        }

        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string>();
        }
    }
}
