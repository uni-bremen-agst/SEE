using System.Collections.Generic;
using SEE.Game.HolisticMetrics;
using SEE.UI;
using SEE.UI.HolisticMetrics;
using SEE.UI.Notification;
using SEE.UI.PropertyDialog.HolisticMetrics;
using SEE.Utils;
using SEE.Utils.History;

namespace SEE.Controls.Actions.HolisticMetrics
{
    /// <summary>
    /// Saves a metrics board's configuration to a file.
    /// </summary>
    public class SaveBoardAction : AbstractPlayerAction
    {
        /// <summary>
        /// The path to the prefab.
        /// </summary>
        private const string buttonPath = "Prefabs/HolisticMetrics/SceneComponents/SaveBoardButton";

        /// <summary>
        /// This field can hold a reference to the dialog that the player will see in the process of executing this
        /// action.
        /// </summary>
        private SaveBoardDialog saveBoardDialog;

        /// <summary>
        /// The controller of the button, in the <see cref="Update"/> method we will "ask" this if the button has been
        /// clicked.
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
        /// This struct can store all the information needed to revert or repeat a <see cref="SaveBoardAction"/>.
        /// </summary>
        private readonly struct Memento
        {
            /// <summary>
            /// The name of the file in which the board's config has been written.
            /// </summary>
            internal readonly string Filename;

            /// <summary>
            /// The WidgetsManager of the board of which the configuration was saved here.
            /// </summary>
            internal readonly WidgetsManager WidgetsManager;

            /// <summary>
            /// The constructor of this struct.
            /// </summary>
            /// <param name="filename">The filename to save into this Memento.</param>
            /// <param name="widgetsManager">The WidgetsManager to save into this file.</param>
            internal Memento(string filename, WidgetsManager widgetsManager)
            {
                Filename = filename;
                WidgetsManager = widgetsManager;
            }
        }

        /// <summary>
        /// Adds a button to the player's UI. That button opens a dialog that lets the player select a metrics board to
        /// save.
        /// </summary>
        public override void Start()
        {
            buttonController = PrefabInstantiator
                .InstantiatePrefab(buttonPath, UICanvas.Canvas.transform, false)
                .GetComponent<LoadBoardButtonController>();
        }

        /// <summary>
        /// This method manages the player's interaction with the mode <see cref="ActionStateType.SaveBoard"/>.
        /// </summary>
        /// <returns>Whether this action is finished.</returns>
        public override bool Update()
        {
            switch (progress)
            {
                case ProgressState.WaitingForClick:
                    if (buttonController.IsClicked())
                    {
                        if (BoardsManager.GetNames().Length == 0)
                        {
                            ShowNotification.Info("No boards in the scene",
                                "There are no boards in the scene that could be saved.");
                            return false;
                        }
                        saveBoardDialog = new SaveBoardDialog();
                        saveBoardDialog.Open();
                        progress = ProgressState.WaitingForInput;
                    }
                    return false;

                case ProgressState.WaitingForInput:
                    if (saveBoardDialog.GetUserInput(out string filename,
                                                     out WidgetsManager widgetsManager))
                    {
                        memento = new Memento(filename, widgetsManager);
                        ConfigManager.SaveBoard(memento.WidgetsManager, memento.Filename);
                        CurrentState = IReversibleAction.Progress.Completed;
                        progress = ProgressState.Finished;
                        return true;
                    }

                    if (saveBoardDialog.WasCanceled())
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
        /// Removes the button (that needs to be clicked to save a metrics board into a file) from the player's UI.
        /// </summary>
        public override void Stop()
        {
            Destroyer.Destroy(buttonController.gameObject);
        }

        /// <summary>
        /// Reverts this action, i.e., deletes the file in which the board configuration was saved.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            ConfigManager.DeleteBoard(memento.Filename);
        }

        /// <summary>
        /// Repeats this action, i.e., saves this board again with the same filename that was given by the player
        /// initially.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            ConfigManager.SaveBoard(memento.WidgetsManager, memento.Filename);
        }

        /// <summary>
        /// Returns a new instance of <see cref="SaveBoardAction"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new SaveBoardAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="SaveBoardAction"/>.
        /// </summary>
        /// <returns>New instance.</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this class.
        /// </summary>
        /// <returns><see cref="ActionStateType.SaveBoard"/>.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.SaveBoard;
        }

        /// <summary>
        /// Returns (in a HashSet) the name of the file into which the config was written.
        /// </summary>
        /// <returns>A HashSet with one entry which is the name of the file into which the config was written.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string> { memento.Filename };
        }
    }
}
