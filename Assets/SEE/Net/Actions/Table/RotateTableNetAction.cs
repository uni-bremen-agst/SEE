using Cysharp.Threading.Tasks;
using SEE.Game.Table;
using UnityEngine;
using UnityEngine.UIElements;

namespace SEE.Net.Actions.Table
{
    /// <summary>
    /// Rotates a table on each client.
    /// </summary>
    public class RotateTableNetAction : TableNetAction
    {
        /// <summary>
        /// The euler angles to which the table should be rotated.
        /// </summary>
        public Vector3 EulerAngles;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="tableID">The table id.</param>
        /// <param name="eulerAngles">The euler angles to which the table should be rotated</param>
        public RotateTableNetAction(string tableID, Vector3 eulerAngles) : base(tableID)
        {
            EulerAngles = eulerAngles;
        }

        /// <summary>
        /// Rotates the table to the given euler angles on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            base.ExecuteOnClient();
            GameTableManager.Rotate(Table, EulerAngles);
        }
    }
}