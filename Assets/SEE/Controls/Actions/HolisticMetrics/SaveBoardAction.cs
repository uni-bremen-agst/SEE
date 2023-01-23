using System.Collections.Generic;
using SEE.Game.HolisticMetrics;
using SEE.Utils;

namespace SEE.Controls.Actions.HolisticMetrics
{
    public class SaveBoardAction : AbstractPlayerAction
    {
        private Memento memento;

        private struct Memento
        {
            internal readonly string filename;
            
            internal readonly BoardConfig config;

            internal Memento(string filename, BoardConfig config)
            {
                this.filename = filename;
                this.config = config;
            }
        }
        
        public override bool Update()
        {
            throw new System.NotImplementedException();
        }
        
        public static ReversibleAction CreateReversibleAction()
        {
            return new SaveBoardAction();
        }
        
        public override ReversibleAction NewInstance()
        {
            return CreateReversibleAction();
        }

        public override ActionStateType GetActionStateType()
        {
            return ActionStateType.SaveBoard;
        }
        
        public override HashSet<string> GetChangedObjects()
        {
            return new HashSet<string> { memento.filename };
        }
    }
}