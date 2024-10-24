using SEE.Game;
using SEE.Game.City;
using SEE.Game.SceneManipulation;
using SEE.GameObjects;
using SEE.Utils;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// This class propagates a <see cref="DeleteAction"/> to all clients in the network.
    /// </summary>
    public class DeleteRootNetAction : AbstractNetAction
    {
        // Note: All attributes are made public so that they will be serialized
        // for the network transfer.

        /// <summary>
        /// The unique name of the gameObject of a node or edge that needs to be deleted.
        /// </summary>
        public string GameObjectID;

        /// <summary>
        /// Creates a new DeleteNetAction.
        /// </summary>
        /// <param name="gameObjectID">the unique name of the gameObject of a node or edge
        /// that has to be deleted</param>
        public DeleteRootNetAction(string gameObjectID) : base()
        {
            GameObjectID = gameObjectID;
        }

        /// <summary>
        /// Things to execute on the server (none for this class). Necessary because it is abstract
        /// in the superclass.
        /// </summary>
        public override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }

        /// <summary>
        /// Deletes the game object identified by <see cref="GameObjectID"/> on each client.
        /// </summary>
        public override void ExecuteOnClient()
        {
            GameObject gameObject = GraphElementIDMap.Find(GameObjectID, mustFindElement: true);
#pragma warning disable VSTHRD110
            Transform cityHolder = gameObject.transform.parent;
            if (cityHolder.GetComponent<CitySelectionManager>() != null)
            {
                cityHolder.GetComponent<CitySelectionManager>().enabled = true;
            }
            if (cityHolder.GetComponent<AbstractSEECity>() is SEEReflexionCity)
            {
                Destroyer.Destroy(cityHolder.GetComponent<ReflexionVisualization>());
                // TODO: what is with the EdgeMeshScheduler component?
                Destroyer.Destroy(cityHolder.GetComponent<EdgeMeshScheduler>());
            }
            Destroyer.Destroy(gameObject);
#pragma warning restore VSTHRD110
        }
    }
}
