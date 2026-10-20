using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
///broadcast an action of the local player to all clients, that is, to replicate
///the action in all client code cities such that all players always have a consistent view of the SEE scenery
/// </summary>
/// 
namespace SEE.Net.Actions.GraphElement

{
    internal class MarkNetAction : NodeNetAction
    {
        public MarkNetAction(string gameNodeID) : base(gameNodeID)
        {

        }

        public override void ExecuteOnClient()
        {
            throw new NotImplementedException();
        }
    }
}
