using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SEE.Controls.Actions
{
    public class NewNodeHoverAction : InteractableObjectHoveringAction
    {
        protected override void Awake()
        {
            base.Awake();  // Must be called to register for the hovering events.
            //isLeaf = SceneQueries.IsLeaf(gameObject);
            //GameObject codeCityObject = SceneQueries.GetCodeCity(gameObject.transform)?.gameObject;
            //Assert.IsTrue(codeCityObject != null);
            //codeCityObject.TryGetComponent(out city);
        }
        /// <summary>
        /// Creates a text label above the object with its node's SourceName if the label doesn't exist yet.
        /// </summary>
        /// <param name="isOwner">true if a local user initiated this call</param
        protected override void On(bool isOwner)
        {
            //highlight node
            Debug.Log("HOVER");
        }
        /// <summary>
        /// Destroys the text label above the object if it exists.
        /// 
        ///  <seealso cref="On"/>
        /// </summary>
        /// <param name="isOwner">true if a local user initiated this call</param>
        protected override void Off(bool isOwner)
        {
            Debug.Log("KEIN HOVER");
        }

        ///<summary>
        /// Selects the gameObject which the Cursor Hovers over
        /// </summary>
        /// <returns>The GameObject its hover Over</returns> 
        public GameObject selectGameObject()
        {
            //todo
            return null;
        }

    }
}
