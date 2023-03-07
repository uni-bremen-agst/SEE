using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Autohand {
    public class AutoHandPlayerForceArea : MonoBehaviour
    {
        public AutoHandPlayer player;
        public float force = 1;
        public ForceMode forceMode = ForceMode.Force;
        public LayerMask layers = ~0;
        Collider[] colliders = new Collider[30];

        private void FixedUpdate()
        {
            var direction = new Vector3 { [player.bodyCollider.direction] = 1 };
            var offset = player.bodyCollider.height / 2 - player.bodyCollider.radius;
            var localPoint0 = player.bodyCollider.center - direction * offset;
            var localPoint1 = player.bodyCollider.center + direction * offset;
            var point0 = transform.TransformPoint(localPoint0);
            var point1 = transform.TransformPoint(localPoint1); 
            var r = transform.TransformVector(player.bodyCollider.radius, player.bodyCollider.radius, player.bodyCollider.radius);
            var radius = Enumerable.Range(0, 3).Select(xyz => xyz == player.bodyCollider.direction ? 0 : r[xyz])
                .Select(Mathf.Abs).Max();

            var overlaps = Physics.OverlapCapsuleNonAlloc(point0, point1, radius, colliders, layers);
            for (int i = 0; i < overlaps; i++)
            {
                if (colliders[i].attachedRigidbody != null)
                {
                    if (colliders[i].attachedRigidbody != player.body && colliders[i].attachedRigidbody != player.handRight.body && colliders[i].attachedRigidbody != player.handLeft.body)
                    {
                        var adjustedForce = (colliders[i].transform.position - player.transform.position).normalized * force;
                        adjustedForce.y /= 10f;
                        colliders[i].attachedRigidbody.AddForce(adjustedForce, forceMode);
                    }
                }
            }
        }
    }
}