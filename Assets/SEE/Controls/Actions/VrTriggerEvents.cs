using SEE.GO;
 using UnityEngine;

 namespace SEE.Controls.Actions
 {
     public class VrTriggerEvents : MonoBehaviour
     {
         void OnCollisionEnter(Collision collision)
         {
             if (gameObject.TryGetComponent(out NodeRef component))
             {
                 Debug.LogWarning("collider with: " + collision.gameObject.name);
             }
         }
     }
 }