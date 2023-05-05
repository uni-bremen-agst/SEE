using System.Collections.Generic;
using System.Linq;
using SEE.Game;
using SEE.Game.Operator;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Net.Actions
{
    /// <summary>
    /// Propagates the rotation of a game node through the network.
    /// </summary>
    internal class RotateNodeNetAction : AbstractNetAction
    {
        /// <summary>
        /// The unique names of the gameObjects of a node that need to be rotated.
        /// </summary>
        public List<string> GameObjectIDs;

        /// <summary>
        /// The rotation of the game object.
        /// </summary>
        public List<Quaternion> RotationList;

        public RotateNodeNetAction(IEnumerable<GameObject> nodes)
        {
            IList<GameObject> gameObjects = nodes.ToList();
            GameObjectIDs = gameObjects.Select(x => x.name).ToList();
            RotationList = gameObjects.Select(x => x.transform.rotation).ToList();
        }

        /// <summary>
        /// Rotation of node in all clients except the requesting client.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            if (!IsRequester())
            {
                int index = 0;
                foreach (string id in GameObjectIDs)
                {
                    GameObject gameObject = Find(id);
                    NodeOperator nodeOperator = gameObject.AddOrGetComponent<NodeOperator>();
                    nodeOperator.RotateTo(RotationList[index++], 0);
                }
            }
        }

        protected override void ExecuteOnServer()
        {
            // Intentionally left blank.
        }
    }
}