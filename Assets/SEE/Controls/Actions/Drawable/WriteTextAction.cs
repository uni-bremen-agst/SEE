using SEE.Game.Drawable;
using SEE.Game.Drawable.Configurations;
using SEE.UI.Notification;
using SEE.Net.Actions.Drawable;
using SEE.UI.Drawable;
using SEE.UI.Menu.Drawable;
using SEE.UI.PropertyDialog.Drawable;
using SEE.Utils;
using System.Collections.Generic;
using UnityEngine;
using SEE.Utils.History;
using SEE.Game.Drawable.ValueHolders;
using SEE.Game.Drawable.ActionHelpers;
using SEE.UI;

namespace SEE.Controls.Actions.Drawable
{
    /// <summary>
    /// Adds a text to a drawable.
    /// </summary>
    public class WriteTextAction : DrawableAction
    {
        /// <summary>
        /// True if we are currently at the first start of this action.
        /// </summary>
        private static bool firstStart = true;

        /// <summary>
        /// The game object that holds the TextMeshPro component.
        /// </summary>
        private GameObject textObj;

        /// <summary>
        /// The position on the drawable where the text should be displayed.
        /// </summary>
        private Vector3 position;

        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This field can hold a reference to the dialog that the player will see in the
        /// process of executing this.
        /// </summary>
        private WriteEditTextDialog writeTextDialog;

        /// <summary>
        /// Indicates how far this instance has progressed in writing a text on a drawable.
        /// </summary>
        private ProgressState progress = ProgressState.GettingPosition;

        /// <summary>
        /// Represents the different stages of progress of this action.
        /// </summary>
        private enum ProgressState
        {
            GettingPosition,
            GettingText
        }

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a
        /// <see cref="WriteTextAction"/>
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The drawable surface on which the text should be displayed.
            /// </summary>
            public DrawableConfig Surface;

            /// <summary>
            /// The written text.
            /// </summary>
            public TextConf Text;

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="surface">The drawable surface on which the text should be displayed.</param>
            /// <param name="text">The written text</param>
            public Memento(GameObject surface, TextConf text)
            {
                Surface = DrawableConfigManager.GetDrawableConfig(surface);
                Text = text;
            }
        }

        /// <summary>
        /// Resets the action values.
        /// </summary>
        public static void Reset()
        {
            firstStart = true;
        }

        /// <summary>
        /// Enables the text menu.
        /// When it starts the first time, it will add necessary listeners.
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            if (firstStart)
            {
                UICanvas.Canvas.AddComponent<ValueResetter>().SetAllowedState(GetActionStateType());
                TextMenu.EnableForWriting();
                firstStart = false;
            }
            else
            {
                TextMenu.Enable(false);
            }
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.WriteText"/>.
        /// </summary>
        /// <returns>Whether this action is finished</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                switch (progress)
                {
                    /// Block for getting the text position.
                    case ProgressState.GettingPosition:
                        GettingPosition();
                        return false;

                    /// Block for getting the text.
                    case ProgressState.GettingText:
                        return GettingText();
                    default:
                        return false;
                }
            }
            return false;
        }

        /// <summary>
        /// Identifying where the text should be placed.
        /// The <see cref="WriteEditTextDialog"/> is then opened.
        /// The user can enter the text in it.
        /// Changes the progress state to <see cref="ProgressState.GettingText"/>
        /// </summary>
        private void GettingPosition()
        {
            if (Selector.SelectQueryHasOrIsDrawableSurface(out RaycastHit raycastHit))
            {
                Surface = GameFinder.GetDrawableSurface(raycastHit.collider.gameObject);
                position = raycastHit.point;
                progress = ProgressState.GettingText;
                writeTextDialog = new WriteEditTextDialog();
                writeTextDialog.Open();
            }
        }

        /// <summary>
        /// Gets the user input for the drawable text and creates the drawable text if the text is not empty.
        /// Because the mesh renderer of TextMeshPro takes some time to generate the mesh,
        /// the mesh collider of the text must be refreshed.
        /// If the text for the drawable text is not empty, the action will be finished.
        ///
        /// If the dialog was canceled or the user input is empty, then the action is reset.
        /// </summary>
        /// <returns>Whatever the success of creating is.</returns>
        private bool GettingText()
        {
            if (writeTextDialog.TryGetUserInput(out string textOut))
            {
                if (textOut != null && textOut != "")
                {
                    DrawableHolder holder = Surface.GetComponent<DrawableHolder>();
                    textObj = GameTexter.WriteText(Surface, textOut, position,
                        ValueHolder.CurrentPrimaryColor, ValueHolder.CurrentSecondaryColor,
                        TextMenu.IsOutlineEnabled(),
                        ValueHolder.CurrentOutlineThickness, ValueHolder.CurrentFontSize,
                        holder.OrderInLayer, TextMenu.GetFontStyle());
                    new WriteTextNetAction(Surface.name, GameFinder.GetDrawableSurfaceParentName(Surface),
                        TextConf.GetText(textObj)).Execute();
                    memento = new Memento(Surface, TextConf.GetText(textObj));
                    GameTexter.RefreshMeshCollider(textObj);
                    CurrentState = IReversibleAction.Progress.Completed;
                    return true;
                }
                else
                {
                    ShowNotification.Warn("Empty text", "The text to write is empty. Please add one.");
                    progress = ProgressState.GettingPosition;
                    return false;
                }
            }
            /// If the dialog was canceled the action will starts from beginning.
            if (writeTextDialog.WasCanceled())
            {
                progress = ProgressState.GettingPosition;
            }
            return false;
        }

        /// <summary>
        /// Stops the <see cref="WriteTextAction"/>.
        /// Refreshes the mesh collider of the text.
        /// It is necessary because the MeshRenderer needs some time to generate and deploy the mesh.
        /// </summary>
        public override void Stop()
        {
            TextMenu.Instance.Disable();
        }

        /// <summary>
        /// Reverts this action, i.e., deletes the text that was written on the drawable.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            GameObject obj = GameFinder.FindChild(memento.Surface.GetDrawableSurface(), memento.Text.ID);
            new EraseNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.Text.ID).Execute();
            Destroyer.Destroy(obj);
        }

        /// <summary>
        /// Repeats this action, i.e., writes the text again on the drawable.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            GameTexter.ReWriteText(memento.Surface.GetDrawableSurface(), memento.Text);
            new WriteTextNetAction(memento.Surface.ID, memento.Surface.ParentID, memento.Text).Execute();
        }

        /// <summary>
        /// A new instance of <see cref="WriteTextAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="WriteTextAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new WriteTextAction();
        }

        /// <summary>
        /// A new instance of <see cref="WriteTextAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="WriteTextAction"/></returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.WriteText"/></returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.WriteText;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>.
        /// </summary>
        /// <returns>the ID of the created drawable text</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new() { memento.Text.ID };
        }
    }
}
