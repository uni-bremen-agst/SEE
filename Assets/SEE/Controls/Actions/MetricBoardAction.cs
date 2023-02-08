using System.Collections.Generic;
using SEE.Utils;

namespace SEE.Controls.Actions
{
    internal class MetricBoardAction : AbstractPlayerAction
    {
        public override bool Update()
        {
            bool completed = false;
            return completed;
        }

        public override HashSet<string> GetChangedObjects()
        {
            throw new System.NotImplementedException();
        }

        public override ActionStateType GetActionStateType()
        {
            return ActionStateTypes.MetricBoard;
        }

        /// <summary>
        /// Returns a new instance of <see cref="MetricBoardAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public static ReversibleAction CreateReversibleAction()
        {
            return new MetricBoardAction();
        }

        /// <summary>
        /// Returns a new instance of <see cref="MetricBoardAction"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }
    }
}