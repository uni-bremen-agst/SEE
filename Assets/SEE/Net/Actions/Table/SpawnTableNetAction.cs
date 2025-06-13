using SEE.Controls.Actions.Table;
using SEE.Game.Table;
using UnityEngine;

namespace SEE.Net.Actions.Table
{
    /// <summary>
    /// This class is repsonsible for spawn <see cref="SpawnTableAction"/> an table on all clients.
    /// </summary>
    public class SpawnTableNetAction : AbstractNetAction
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
        /// <param name="name">The name of the table.</param>
        /// <param name="position">The position of the table.</param>
        /// <param name="eulerAngles">The euler angles of the table.</param>
        /// <param name="scale">The scale of the table.</param>
        public SpawnTableNetAction(string name, Vector3 position, Vector3 scale)
        {
            Name = name;
            Position = position;
            Scale = scale;
        }

        /// <summary>
        /// Spawns the table on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameTableManager.Respawn(Name, Position, Scale);
        }
    }
}
