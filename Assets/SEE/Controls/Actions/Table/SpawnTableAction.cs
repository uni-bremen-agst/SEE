using SEE.Controls.Actions.Table;
using SEE.Game;
using SEE.Game.Table;
using SEE.GameObjects;
using SEE.Utils;
using SEE.Utils.History;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions.Drawable
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
        /// Saves all the information needed to revert or repeat this action.
        /// </summary>
        private Memento memento;

        /// <summary>
        /// This struct can store all the information needed to revert or repeat a <see cref="SpawnTableAction"/>
        /// </summary>
        private struct Memento
        {

        }

        /// <summary>
        /// Starts the action and spawns a table.
        /// </summary>
        public override void Start()
        {
            base.Start();
            spawnedTable = GameTableManager.Spawn();
        }

        /// <summary>
        /// Stops the action and destroys the table if the action was not completed.
        /// Also removes the table from the <see cref="CitiesHolder"/> component.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            if (!finish)
            {
                if (LocalPlayer.TryGetCitiesHolder(out CitiesHolder citiesHolder))
                {
                    citiesHolder.Cities.Remove(spawnedTable.name);
                }
                Destroyer.Destroy(spawnedTable);

            }
        }

        /// <summary>
        /// Moves the table along the raycast until the user clicks the left mouse button.
        /// </summary>
        /// <returns></returns>
        public override bool Update()
        {
            if (!finish && Raycasting.RaycastAnything(out RaycastHit raycastHit))
            {
                GameTableManager.Move(spawnedTable, raycastHit.point);
                if (SEEInput.LeftMouseDown())
                {
                    finish = true;
                    GameTableManager.FinishSpawn(spawnedTable);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Reverts this action, i.e., TODO.
        /// </summary>
        public override void Undo()
        {
            base.Undo();
        }

        /// <summary>
        /// Repeats this action, i.e., TODO.
        /// </summary>
        public override void Redo()
        {
            base.Redo();
        }

        /// <summary>
        /// A new instance of <see cref="SpawnTableAction"/>.
        /// See <see cref="ReversibleAction.CreateReversibleAction"/>.
        /// </summary>
        /// <returns>new instance of <see cref="SpawnTableAction"/></returns>
        public static IReversibleAction CreateReversibleAction()
        {
            return new SpawnTableAction();
        }

        /// <summary>
        /// A new instance of <see cref="SpawnTableAction"/>.
        /// See <see cref="ReversibleAction.NewInstance"/>.
        /// </summary>
        /// <returns>new instance of <see cref="SpawnTableAction"/></returns>
        public override IReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        /// <summary>
        /// Returns the <see cref="ActionStateType"/> of this action.
        /// </summary>
        /// <returns><see cref="ActionStateType.SpawnTable"/></returns>
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
            return new();
        }
    }
}