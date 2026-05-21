using Cysharp.Threading.Tasks;
using SEE.Game.Table;
using SEE.Net.Actions.Table;
using SEE.UI.Notification;
using SEE.Utils;
using SEE.Utils.History;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions.Table
{
    /// <summary>
    /// This class provides the option to spawn a new table.
    /// </summary>
    public class SpawnTableAction : AbstractPlayerAction
    {
        /// <summary>
        /// The spawned table.
        /// </summary>
        private GameObject spawnedTable;

        /// <summary>
        /// Indicator if the action is completed.
        /// </summary>
        private bool finish = false;

        /// <summary>
        /// Indicator whether the action waited for synchronization after the <see cref="Start"/> method.
        /// Without waiting, the table to be moved might not be found by the other connected players.
        /// </summary>
        private bool waitedForSynchronization = false;

        /// <summary>
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="SpawnTableAction"/>
        /// </summary>
        private struct Memento
        {
            /// <summary>
            /// The name of the table.
            /// </summary>
            public string Name;

            /// <summary>
            /// The position of the table.
            /// </summary>
            public Vector3 Position;

            /// <summary>
            /// The scale of the table.
            /// </summary>
            public Vector3 Scale;

            /// <summary>
            /// The constructor.
            /// </summary>
            /// <param name="table">The spawned table.</param>
            public Memento(GameObject table)
            {
                Name = table.name;
                Position = table.transform.position;
                Scale = table.transform.localScale;
            }
        }

        /// <summary>
        /// Starts the action and spawns a table.
        /// </summary>
        public override void Start()
        {
            base.Start();
            spawnedTable = GameTableManager.Spawn();
            new SpawnTableNetAction(spawnedTable.name,
                spawnedTable.transform.position,
                spawnedTable.transform.localScale).Execute();
        }

        /// <summary>
        /// Stops the action and destroys the table if the action was not completed.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            if (!finish && spawnedTable != null)
            {
                string name = spawnedTable.name;
                GameTableManager.Destroy(spawnedTable);
                new DestroyTableNetAction(name).Execute();
            }
        }

        /// <summary>
        /// Moves the table along the raycast until the user clicks the left mouse button.
        /// Ensures that the table does not overlap with any other object.
        /// </summary>
        /// <returns>True if action is finished.</returns>
        public override bool Update()
        {
            if (!Raycasting.IsMouseOverGUI())
            {
                if (!waitedForSynchronization)
                {
                    Wait().Forget();
                }
                if (waitedForSynchronization && !finish && Raycasting.RaycastAnything(out RaycastHit raycastHit))
                {
                    GameTableManager.Move(spawnedTable, raycastHit.point);
                    new MoveTableOnlyNetAction(spawnedTable.name, spawnedTable.transform.position).Execute();
                    if (SEEInput.LeftMouseDown())
                    {
                        if (spawnedTable.TryGetComponent<CollisionDetectionManager>(out CollisionDetectionManager cdManager)
                            && cdManager.IsInCollision())
                        {
                            ShowNotification.Warn("Table cannot be placed",
                                "The table cannot be placed because it is colliding with another object.");
                            return false;
                        }
                        finish = true;
                        GameTableManager.FinishSpawn(spawnedTable);
                        memento = new(spawnedTable);
                        CurrentState = IReversibleAction.Progress.Completed;
                        return true;
                    }
                }
            }
            return false;

            async UniTask Wait()
            {
                await UniTask.Yield();
                waitedForSynchronization = true;
            }
        }

        /// <summary>
        /// Reverts this action, i.e., it destroys the spawned table.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
            if (spawnedTable != null)
            {
                new DestroyTableNetAction(spawnedTable.name).Execute();
                GameTableManager.Destroy(spawnedTable);
            }
        }

        /// <summary>
        /// Repeats this action, i.e., it respawns the table.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
            if (spawnedTable == null)
            {
                spawnedTable = GameTableManager.Respawn(memento.Name,
                    memento.Position, memento.Scale);
                new SpawnTableNetAction(memento.Name, memento.Position, memento.Scale).Execute();
            }
        }

        /// <summary>
        /// A new instance of <see cref="SpawnTableAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>New instance of <see cref="SpawnTableAction"/>.</returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new SpawnTableAction();
        }

        /// <summary>
        /// A new instance of <see cref="SpawnTableAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>New instance of <see cref="SpawnTableAction"/>.</returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.SpawnTable"/>.</returns>
        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.SpawnTable;
        }

        /// <summary>
        /// The set of IDs of all gameObjects changed by this action.
        /// <see cref="ReversibleAction.GetActionStateType"/>
        /// </summary>
        /// <returns>The object id of the changed object.</returns>
        public override HashSet<string> GetChangedObjects()
        {
            return new() { spawnedTable.name };
        }
    }
}
