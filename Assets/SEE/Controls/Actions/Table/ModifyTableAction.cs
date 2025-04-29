using SEE.Game;
using SEE.Game.Drawable;
using SEE.Game.Table;
using SEE.GO;
using SEE.GameObjects;
using SEE.UI.Menu;
using SEE.UI.Notification;
using SEE.Utils;
using SEE.Utils.History;
using System.Collections.Generic;
using UnityEngine;
using MoreLinq;
using SEE.Net.Actions.Table;
using ModifyOperation = SEE.UI.Menu.ModifyTableMenu.ModifyOperation;
using SEE.Game.City;
using System.Linq;

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
            Rotate,
            Scale,
            Delete,
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
        /// This struct can store all the information needed to revert or repeat a <see cref="ModifyTableAction"/>
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
            /// The relevant transform data (position, euler angles and scale).
            /// </summary>
            public struct TransformData
            {
                /// <summary>
                /// The position.
                /// </summary>
                public Vector3 Position;

                /// <summary>
                /// The euler angles.
                /// </summary>
                public Vector3 EulerAngles;

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
                    EulerAngles = transform.eulerAngles;
                    Scale = transform.localScale;
                }
            }

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="table">The modified table.</param>
            /// <param name="operation">The operation that was executed.</param>
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
            if (menu != null)
            {
                menu.Destroy();
            }
            if (modifiedTable != null)
            {
                GameTableManager.DisableEditMode(modifiedTable);
            }
            if (LocalPlayer.TryGetCitiesHolder(out CitiesHolder citiesHolder))
            {
                previousCSMState.Keys.ForEach(city =>
                    city.GetComponent<CitySelectionManager>().enabled = previousCSMState[city]);
            }
        }

        /// <summary>
        /// Executes the user's input to modifying the table.
        /// </summary>
        /// <returns>True if the action is completed successfully. Otherwise, false.</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                switch (currentProgressState)
                {
                    case ProgressState.TableSelection:
                        if (SEEInput.LeftMouseDown() && Raycasting.RaycastAnything(out RaycastHit raycastHit)
                            && (raycastHit.collider.gameObject.CompareTag(Tags.CodeCity)
                                || GameFinder.HasParentWithTag(raycastHit.collider.gameObject, Tags.CodeCity))
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
                            currentProgressState = ProgressState.OperationSelection;
                            menu = new();
                        }
                        break;
                    case ProgressState.OperationSelection:
                        OperationSelection();
                        Unselect();
                        break;
                    case ProgressState.Move:
                        DisableCity();
                        if (Raycasting.RaycastAnything(out RaycastHit raycast))
                        {
                            GameTableManager.Move(modifiedTable, raycast.point);
                            new MoveTableNetAction(modifiedTable.name, raycast.point).Execute();
                            if (SEEInput.LeftMouseDown())
                            {
                                if (modifiedTable.GetComponent<CollisionDetectionManager>().IsInCollision())
                                {
                                    ShowNotification.Warn("Table can't be placed",
                                        "The table can't be placed because it is colliding with another object.");
                                    return false;
                                }
                                currentProgressState = ProgressState.Finish;
                            }
                        }
                        break;
                    case ProgressState.Rotate:
                    case ProgressState.Scale:
                    case ProgressState.Delete:
                        break;
                    case ProgressState.Finish:
                        RedrawCity();
                        memento.SetNewData(modifiedTable, executedOperation);
                        CurrentState = IReversibleAction.Progress.Completed;
                        return true;
                }
            }
            return false;
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
        /// Disables the drawn city.
        /// </summary>
        private void DisableCity()
        {
            if (GameFinder.FindChildWithTag(modifiedTable, Tags.CodeCity) is GameObject city
                && city.IsCodeCityDrawnAndActive())
            {
                GameFinder.FindChildWithTag(city, Tags.Node).SetActive(false);
            }
        }

        /// <summary>
        /// Redraws the city if necessary.
        /// </summary>
        private void RedrawCity()
        {
            if (executedOperation != ProgressState.Delete
                && GameFinder.FindChildWithTag(modifiedTable, Tags.CodeCity) is GameObject city
                && city.IsCodeCityDrawn())
            {
                city.GetComponent<SEECity>().ReDrawGraph();
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
                case ModifyOperation.Rotate:
                    return ProgressState.Rotate;
                case ModifyOperation.Scale:
                    return ProgressState.Scale;
                default:
                    return ProgressState.OperationSelection;
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
                GameTableManager.Move(modifiedTable, memento.Old.Position);
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
                GameTableManager.Move(modifiedTable, memento.New.Position);
            }
        }

        /// <summary>
        /// A new instance of <see cref="ModifyTableAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="ModifyTableAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new ModifyTableAction();
        }

        /// <summary>
        /// A new instance of <see cref="ModifyTableAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="ModifyTableAction"/></returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.ModifyTable"/></returns>
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
            return modifiedTable != null?
                new() { modifiedTable.name } : new();
        }

    }
}
