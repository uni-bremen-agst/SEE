using UnityEngine;

namespace SEE.Net.Actions.Table
{
    public abstract class MoveTableNetAction : TableNetAction
    {
        /// <summary>
        /// The position to which the table should be moved.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// The constructor.
        /// </summary>
        /// <param name="tableID">The table id.</param>
        /// <param name="position">The position to which the table should be moved</param>
        public MoveTableNetAction(string tableID, Vector3 position) : base(tableID)
        {
            Position = position;
        }
    }
}
