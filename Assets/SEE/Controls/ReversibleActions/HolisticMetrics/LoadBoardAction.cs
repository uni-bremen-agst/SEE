using System;
using System.Collections.Generic;
using SEE.Game.HolisticMetrics;
using SEE.UI.HolisticMetrics;
using SEE.UI.Notification;
using SEE.UI.PropertyDialog.HolisticMetrics;
using SEE.Net.Actions.HolisticMetrics;
using SEE.Utils;
using UnityEngine;
using SEE.Utils.History;
using SEE.UI;

namespace SEE.Controls.Actions.HolisticMetrics
{
    /// <summary>
    /// Adds a board to the scene from a board config saved in a file on the disk.
    /// </summary>
    public class LoadBoardAction : AbstractPlayerAction
    {
        /// <summary>
        /// The path to the <see cref="button"/> prefab.
        /// </summary>
        private const string buttonPath = "Prefabs/HolisticMetrics/SceneComponents/LoadBoardButton";

        /// <summary>
        /// A button on the player's UI. When the button is clicked, the player will see a dialog in which he can
        /// proceed to save a metrics board.
        /// </summary>
        private GameObject button;

        /// <summary>
        /// This field can hold a reference to the dialog that the player will see in the process of executing this
        /// action.
        /// </summary>
        private LoadBoardDialog loadBoardDialog;

        /// <summary>
        /// The controller of the <see cref="button"/>. We will "ask" this whether the player has clicked the button.
        /// </summary>
        private LoadBoardButtonController buttonController;

        /// <summary>
        /// Stores the current progress of this action.
        /// </summary>
        private ProgressState progress = ProgressState.WaitingForClick;

        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// Represents the progress that this action has made.
        /// </summary>
        private enum ProgressState
        {
            WaitingForClick,
            WaitingForInput,
            Finished
        }

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="LoadBoardAction"/>.
        /// </summary>
        private readonly struct Memento
        {
            /// <summary>
            /// The config of the board that has been loaded.
            /// </summary>
            internal readonly BoardConfig Config;

            /// <summary>
            /// The constructor, which simply assigns its only parameter to a field in this class.
            /// </summary>
            /// <param name="config">The config to save in this Memento.</param>
            internal Memento(BoardConfig config)
            {
                Config = config;
            }
        }

        /// <summary>
        /// Adds the <see cref="button"/> to the player's UI.
        /// </summary>
        public override void Start()
        {
            button = PrefabInstantiator.InstantiatePrefab(buttonPath, UICanvas.Canvas.transform, false);
            buttonController = button.GetComponent<LoadBoardButtonController>();
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.LoadBoard"/>.
        /// </summary>
        /// <returns>Whether this action is finished.</returns>
        public override bool Update()
        {
            switch (progress)
            {
                case ProgressState.WaitingForClick:
                    if (buttonController.IsClicked())
                    {
                        loadBoardDialog = new LoadBoardDialog();
                        loadBoardDialog.Open();
                        progress = ProgressState.WaitingForInput;
                    }
                    return false;

                case ProgressState.WaitingForInput:
                    if (loadBoardDialog.TryGetFilename(out string filename))
                    {
                        try
                        {
                            BoardConfig config = ConfigManager.LoadBoard(filename);
                            memento = new Memento(config);
                            BoardsManager.Create(memento.Config);
                            new CreateBoardNetAction(memento.Config).Execute();
                            CurrentState = IReversibleAction.Progress.Completed;
                            return true;
                        }
                        catch (Exception exception)
                        {
                            ShowNotification.Error("Loading failure", $"Could not load board from {filename}: {exception.Message}");
                            throw exception;
                        }
                    }
                    else if (loadBoardDialog.WasCanceled())
                    {
                        progress = ProgressState.WaitingForClick;
                    }
                    return false;

                case ProgressState.Finished:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Removes the <see cref="button"/> from the player's UI.
        /// </summary>
        public override void Stop()
        {
            Destroyer.Destroy(button);
        }

        /// <summary>
        /// Reverts this instance of the action, i.e., deletes the board that was loaded from the scene.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            BoardsManager.Delete(memento.Config.Title);
            new DeleteBoardNetAction(memento.Config.Title).Execute();
        }

        /// <summary>
        /// Repeats this action, i.e., creates the board again.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            BoardsManager.Create(memento.Config);
            new CreateBoardNetAction(memento.Config).Execute();
        }

        /// <summary>
        /// Returns a new instance of <see cref="LoadBoardAction"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new LoadBoardAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="LoadBoardAction"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this class.
        /// </summary>
        /// <returns><see cref="ActionStateType.LoadBoard"/>.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.LoadBoard;
        }

        /// <summary>
        /// Returns the name of the board that was loaded / added to the scene by this action.
        /// </summary>
        /// <returns>The name of the board that was loaded / added to the scene by this action, inside a HashSet of
        /// strings.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string> { memento.Config.Title };
        }
    }
}
