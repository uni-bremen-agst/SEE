using Cysharp.Threading.Tasks;
using MoreLinq;
using SEE.Game;
using SEE.Game.City;
using SEE.Game.Table;
using SEE.GameObjects;
using SEE.GO;
using SEE.Net.Actions.Table;
using SEE.UI.Menu;
using SEE.UI.Menu.Table;
using SEE.UI.Notification;
using SEE.Utils;
using SEE.Utils.History;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ModifyOperation = SEE.UI.Menu.Table.ModifyTableMenu.ModifyOperation;

namespace SEE.Controls.Actions.Table
{
    /// <summary>
    /// This action provides functionality to move, rotate, scale and delete
    /// existing tables.
    /// </summary>
    public class ModifyTableAction : AbstractPlayerAction
    {
        /// <summary>
        /// The progress states of this action.
        /// </summary>
        private enum ProgressState
        {
            None,
            TableSelection,
            OperationSelection,
            Move,
            Scale,
            Delete,
            Wait,
            Finish
        }

        /// <summary>
        /// The current progress state.
        /// </summary>
        private ProgressState currentProgressState = ProgressState.TableSelection;

        /// <summary>
        /// The executed operation.
        /// </summary>
        private ProgressState executedOperation = ProgressState.None;

        /// <summary>
        /// The menu for this action.
        /// </summary>
        private ModifyTableMenu menu;

        /// <summary>
        /// The menu for scaling.
        /// </summary>
        private ScaleTableMenu scaleMenu;

        /// <summary>
        /// The modified table.
        /// </summary>
        private GameObject modifiedTable;

        /// <summary>
        /// This dictionary contains the previous state of each city's <see cref="CitySelectionManager"/>.
        /// </summary>
        private readonly Dictionary<GameObject, bool> previousCSMState = new();

        /// <summary>
        /// Saves all the information to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// Memento to store all the information needed to revert or repeat a <see cref="ModifyTableAction"/>
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The name of the modified table.
            /// </summary>
            public string Name;

            /// <summary>
            /// The old transform data of the table.
            /// </summary>
            public TransformData Old;

            /// <summary>
            /// The new transform data of the table.
            /// </summary>
            public TransformData New;

            /// <summary>
            /// The operation that was executed.
            /// </summary>
            public ProgressState Operation;

            /// <summary>
            /// Indicates whether the new transform data has been set.
            /// </summary>
            private bool newDataWasSet;

            /// <summary>
            /// The relevant transform data (position, euler angles, and scale).
            /// </summary>
            public struct TransformData
            {
                /// <summary>
                /// The position.
                /// </summary>
                public Vector3 Position;

                /// <summary>
                /// The scale.
                /// </summary>
                public Vector3 Scale;

                /// <summary>
                /// The constructor.
                /// </summary>
                /// <param name="table">The modified table.</param>
                public TransformData(GameObject table)
                {
                    Transform transform = table.transform;
                    Position = transform.position;
                    Scale = transform.localScale;
                }
            }

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="table">The modified table.</param>
            public Memento(GameObject table)
            {
                Name = table.name;
                Old = new(table);
                New = new();
                Operation = ProgressState.None;
                newDataWasSet = false;
            }

            /// <summary>
            /// Sets the new transform data of the given <paramref name="table"/>
            /// after the specified <paramref name="operation"/> is executed.
            /// </summary>
            /// <param name="table">The modified table.</param>
            /// <param name="operation">The operation that was executed.</param>
            public void SetNewData(GameObject table, ProgressState operation)
            {
                if (!newDataWasSet)
                {
                    newDataWasSet = true;
                    New = new(table);
                    Operation = operation;
                }
            }
        }

        /// <summary>
        /// Disables and saves the current state of each city's <see cref="CitySelectionManager"/> component.
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            if (LocalPlayer.TryGetCitiesHolder(out CitiesHolder citiesHolder))
            {
                citiesHolder.Cities.Values.ForEach(city =>
                {
                    previousCSMState.Add(city, city.GetComponent<CitySelectionManager>().enabled);
                    city.GetComponent<CitySelectionManager>().enabled = false;
                });
            }
        }

        /// <summary>
        /// Stops the action and restores the previous state of each city's <see cref="CitySelectionManager"/>.
        /// Also destroys the menu and the <see cref="CollisionDetectionManager"/> component, if present.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            menu?.Destroy();
            scaleMenu?.Destroy();
            if (modifiedTable != null)
            {
                GameTableManager.DisableEditMode(modifiedTable);
            }
            if (LocalPlayer.TryGetCitiesHolder(out CitiesHolder citiesHolder))
            {
                previousCSMState.Keys.ForEach(city =>
                {
                    if (city != null)
                    {
                        city.GetComponent<CitySelectionManager>().enabled = previousCSMState[city];
                    }
                });
            }
            if (currentProgressState != ProgressState.Finish
                && executedOperation != ProgressState.None
                && modifiedTable != null)
            {
                switch (executedOperation)
                {
                    case ProgressState.Move:
                        GameTableManager.MoveIncPortal(modifiedTable, memento.Old.Position);
                        new MoveTableAndPortalNetAction(modifiedTable.name, memento.Old.Position).Execute();
                        break;
                    case ProgressState.Scale:
                        GameTableManager.Scale(modifiedTable, memento.Old.Scale);
                        new ScaleTableNetAction(modifiedTable.name, memento.Old.Scale).Execute();
                        UpdateCity();
                        break;
                }
            }
        }

        /// <summary>
        /// Executes the user's input to modify the table.
        /// </summary>
        /// <returns>True if the action is completed successfully; otherwise false.</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                switch (currentProgressState)
                {
                    case ProgressState.TableSelection:
                        TableSelection();
                        break;
                    case ProgressState.OperationSelection:
                        OperationSelection();
                        Unselect();
                        break;
                    case ProgressState.Move:
                        MoveTable();
                        break;
                    case ProgressState.Scale:
                        ScaleTable();
                        break;
                    case ProgressState.Delete:
                        DeleteTableAsync().Forget();
                        break;
                    case ProgressState.Finish:
                        UpdateCityAndSaveMementoIfNeeded();
                        CurrentState = IReversibleAction.Progress.Completed;
                        return true;
                }
            }
            else
            {
                CheckScaleMenu();
            }
            return false;
        }

        /// <summary>
        /// Provides the selection of a table to be modified.
        /// The table or an object on the table can be chosen for this purpose.
        /// </summary>
        private void TableSelection()
        {
            if (SEEInput.LeftMouseDown() && Raycasting.RaycastAnything(out RaycastHit raycastHit)
                && (raycastHit.collider.gameObject.CompareTag(Tags.CodeCity)
                    ||raycastHit.collider.gameObject.HasParentWithTag(Tags.CodeCity))
                && modifiedTable == null)
            {
                GameObject raycastObj = raycastHit.collider.gameObject;
                GameObject city = raycastObj.CompareTag(Tags.CodeCity) ?
                       raycastObj : raycastObj.ContainingCity().gameObject;
                modifiedTable = city.transform.parent.gameObject;
                GameTableManager.EnableEditMode(modifiedTable);
            }
            if (modifiedTable != null && SEEInput.MouseUp(MouseButton.Left))
            {
                OpenOperationSelection();
            }
        }

        /// <summary>
        /// Changes the progress state to the operation selection and opens the
        /// depending <see cref="menu"/>.
        /// </summary>
        private void OpenOperationSelection()
        {
            currentProgressState = ProgressState.OperationSelection;
            menu = new();
        }

        /// <summary>
        /// Waits for the user's input to select an operation
        /// and then initiates the execution of the chosen operation.
        /// </summary>
        private void OperationSelection()
        {
            if (menu.TryGetInput(out ModifyOperation modifyOperation)
                && modifyOperation != ModifyOperation.Cancel)
            {
                memento = new(modifiedTable);
                executedOperation = DetermineProgressState(modifyOperation);
                currentProgressState = DetermineProgressState(modifyOperation);
                PrepareSelectedOperation();
            }
        }

        /// <summary>
        /// Prepares the selected operation by performing necessary setup steps.
        /// If applicable, it resets a zoom and activates an infinity portal for movement.
        /// </summary>
        private void PrepareSelectedOperation()
        {
            if (modifiedTable.GetComponentInChildren<AbstractSEECity>() is { } city
                && city.gameObject.IsCodeCityDrawn())
            {
                if (LocalPlayer.TryGetZoomActionDesktop(out ZoomActionDesktop zoomActionDesktop))
                {
                    Transform rootTransform = city.gameObject.transform.Cast<Transform>()
                        .First(child => child.gameObject.IsNode());
                    if (zoomActionDesktop.TriggerImmediateReset(rootTransform))
                    {
                        currentProgressState = ProgressState.Wait;
                        WaitForReset(rootTransform).Forget();
                    }
                }
                if (executedOperation == ProgressState.Move)
                {
                    Portal.SetInfinitePortal(city.gameObject);
                    new SetInfinitePortalTableNetAction(modifiedTable.name).Execute();
                }
            }
            else
            {
                DisableCitySelectionManager();
            }
            async UniTask WaitForReset(Transform rootTransform)
            {
                await UniTask.Yield();
                currentProgressState = executedOperation;
            }
        }

        /// <summary>
        /// Unselects the current selected table and resets the progress state.
        /// </summary>
        private void Unselect()
        {
            if (SEEInput.LeftMouseDown() && Raycasting.RaycastAnything(out RaycastHit raycastHit)
                    && !raycastHit.collider.gameObject.Equals(modifiedTable)
                || menu.TryGetInput(out ModifyOperation operation)
                    && operation == ModifyOperation.Cancel)
            {
                GameTableManager.DisableEditMode(modifiedTable);
                modifiedTable = null;
                menu.Destroy();
                currentProgressState = ProgressState.TableSelection;
            }
        }

        /// <summary>
        /// Calls <see cref="GameTableManager.DisableCSM(GameObject)"/>
        /// and the corresponding network action.
        /// </summary>
        private void DisableCitySelectionManager()
        {
            GameTableManager.DisableCSM(modifiedTable);
            new DisableCSMTableNetAction(modifiedTable.name).Execute();
        }

        /// <summary>
        /// Calls <see cref="GameTableManager.EnableCity(GameObject)"/>
        /// and the corresponding network action.
        /// </summary>
        private void UpdateCity()
        {
            GameTableManager.EnableCity(modifiedTable);
            new EnableCityTableNetAction(modifiedTable.name).Execute();
        }

        /// <summary>
        /// Calls <see cref="GameTableManager.EnableCity(GameObject)"/> if necessary
        /// and performs the corresponding network action.
        /// Also saves the new memento data to repeat this action.
        /// If a move was executed and the city is drawn, the portal will be adjusted accordingly.
        /// </summary>
        private void UpdateCityAndSaveMementoIfNeeded()
        {
            if (executedOperation != ProgressState.Delete)
            {
                UpdateCity();
                memento.SetNewData(modifiedTable, executedOperation);
            }
            else
            {
                memento.Operation = executedOperation;
            }
        }

        /// <summary>
        /// Converts a <paramref name="modifyOperation"/> to the corresponding <see cref="ProgressState"/>.
        /// </summary>
        /// <param name="modifyOperation">The operation to be converted.</param>
        /// <returns>The corresponding <see cref="ProgressState"/> value based on the selected operation.</returns>
        private ProgressState DetermineProgressState(ModifyOperation modifyOperation)
        {
            switch (modifyOperation)
            {
                case ModifyOperation.Move:
                    return ProgressState.Move;
                case ModifyOperation.Delete:
                    return ProgressState.Delete;
                case ModifyOperation.Scale:
                    return ProgressState.Scale;
                default:
                    return ProgressState.OperationSelection;
            }
        }

        /// <summary>
        /// Checks for a collision when the left mouse button is pressed.
        /// </summary>
        private void CheckCollisionWithLeftMouseButton()
        {
            if (SEEInput.LeftMouseDown())
            {
                CheckCollision();
            }
        }

        /// <summary>
        /// Checks for a collision.
        /// If a collision is detected, a warning is shown.
        /// Otherwise, the current progress state is set to finish.
        /// </summary>
        private void CheckCollision()
        {
            if (modifiedTable.GetComponent<CollisionDetectionManager>().IsInCollision())
            {
                ShowNotification.Warn("Table cannot be placed",
                    "The table cannot be placed because it is colliding with another object.");
            }
            else
            {
                currentProgressState = ProgressState.Finish;
                scaleMenu?.Destroy();
            }
        }

        /// <summary>
        /// Moves the table based on a raycast and checks for collisions.
        /// </summary>
        private void MoveTable()
        {
            if (Raycasting.RaycastAnything(out RaycastHit raycast))
            {
                GameTableManager.Move(modifiedTable, raycast.point);
                new MoveTableOnlyNetAction(modifiedTable.name, raycast.point).Execute();
                CheckCollisionWithLeftMouseButton();
            }
        }

        /// <summary>
        /// Scales the table and checks for collisions.
        /// </summary>
        private void ScaleTable()
        {
            InitScaleMenu();
            CheckCollisionWithLeftMouseButton();
        }

        /// <summary>
        /// Instantiates the scale menu.
        /// </summary>
        private void InitScaleMenu()
        {
            scaleMenu ??= new ScaleTableMenu(modifiedTable);
        }

        /// <summary>
        /// Listen to the dialog action buttons of the scale menu.
        /// </summary>
        private void CheckScaleMenu()
        {
            if (scaleMenu != null && currentProgressState == ProgressState.Scale)
            {
                if (scaleMenu.TryGetFinish())
                {
                    CheckCollision();
                }
                if (scaleMenu.TryGetCanceled())
                {
                    GameTableManager.Scale(modifiedTable, memento.Old.Scale);
                    new ScaleTableNetAction(modifiedTable.name, memento.Old.Scale).Execute();
                    scaleMenu = null;
                    OpenOperationSelection();
                }
            }
        }

        /// <summary>
        /// Deletes the table.
        /// </summary>
        private async UniTask DeleteTableAsync()
        {
            currentProgressState = ProgressState.Wait;
            if (!modifiedTable.GetComponentInChildren<SEECity>()
                || !modifiedTable.GetComponentInChildren<SEECity>().gameObject.IsCodeCityDrawnAndActive())
            {
                Delete();
            }
            else
            {
                string deleteMessage = "Do you really want to delete this table?\nThe city cannot be rebuilt using undo.";
                if (await ConfirmDialog.ConfirmAsync(ConfirmConfiguration.Delete(deleteMessage)))
                {
                    Delete();
                }
                else
                {
                    currentProgressState = ProgressState.OperationSelection;
                    menu = new();
                }
            }
            void Delete()
            {
                new DestroyTableNetAction(modifiedTable.name).Execute();
                GameTableManager.Destroy(modifiedTable);
                currentProgressState = ProgressState.Finish;
            }
        }

        /// <summary>
        /// Reverts this action, i.e., it undoes the modification changes.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (LocalPlayer.TryGetCitiesHolder(out CitiesHolder citiesHolder))
            {
                modifiedTable = citiesHolder.FindTable(memento.Name);
                switch (memento.Operation)
                {
                    case ProgressState.Move:
                        GameTableManager.MoveIncPortal(modifiedTable, memento.Old.Position);
                        new MoveTableAndPortalNetAction(modifiedTable.name, memento.Old.Position).Execute();
                        break;
                    case ProgressState.Scale:
                        GameTableManager.Scale(modifiedTable, memento.Old.Scale);
                        new ScaleTableNetAction(modifiedTable.name, memento.Old.Scale).Execute();
                        UpdateCity();
                        break;
                    case ProgressState.Delete:
                        GameTableManager.Respawn(memento.Name, memento.Old.Position, memento.Old.Scale);
                        new SpawnTableNetAction(memento.Name, memento.Old.Position, memento.Old.Scale).Execute();
                        break;
                }
            }
        }

        /// <summary>
        /// Repeats this action, i.e., it redoes the modification changes.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            if (LocalPlayer.TryGetCitiesHolder(out CitiesHolder citiesHolder))
            {
                modifiedTable = citiesHolder.FindTable(memento.Name);
                switch (memento.Operation)
                {
                    case ProgressState.Move:
                        GameTableManager.MoveIncPortal(modifiedTable, memento.New.Position);
                        new MoveTableAndPortalNetAction(modifiedTable.name, memento.New.Position).Execute();
                        break;
                    case ProgressState.Scale:
                        GameTableManager.Scale(modifiedTable, memento.New.Scale);
                        new ScaleTableNetAction(modifiedTable.name, memento.New.Scale).Execute();
                        UpdateCity();
                        break;
                    case ProgressState.Delete:
                        new DestroyTableNetAction(modifiedTable.name).Execute();
                        GameTableManager.Destroy(modifiedTable);
                        break;
                }
            }
        }

        /// <summary>
        /// A new instance of <see cref="ModifyTableAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>New instance of <see cref="ModifyTableAction"/>.</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new ModifyTableAction();
        }

        /// <summary>
        /// A new instance of <see cref="ModifyTableAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>New instance of <see cref="ModifyTableAction"/>.</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.ModifyTable"/>.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.ModifyTable;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// </summary>
        /// <returns>The object id of the changed object.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new() { memento.Name };
        }
    }
}
