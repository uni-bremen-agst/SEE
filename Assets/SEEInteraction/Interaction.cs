using UnityEngine;

namespace SEE
{

    public abstract class Interaction
    {
        public void Execute()
        {
            Net.Network.ExecuteInteraction(this);
        }

        internal abstract void ExecuteLocally();
    }
    
    public class DeleteBuildingInteraction : Interaction
    {
        public readonly GameObject building;

        public DeleteBuildingInteraction(GameObject building)
        {
            this.building = building;
        }

        internal override void ExecuteLocally()
        {
            Object.Destroy(building);
        }
    }

}
