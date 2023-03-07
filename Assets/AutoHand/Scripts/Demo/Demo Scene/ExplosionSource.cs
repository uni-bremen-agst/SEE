using UnityEngine;

namespace Autohand.Demo{
    public class ExplosionSource : MonoBehaviour{
        public float radius = 1;
        public float force = 10;

        public void Explode(bool destroy) {
            var hits = Physics.OverlapSphere(transform.position, radius);
            foreach(var hit in hits) {
                var rb = hit.GetComponent<Rigidbody>();
                if(rb != null)
                    rb.AddExplosionForce(force, transform.position, radius);
            }
            if(destroy)
                Destroy(gameObject);
        }

        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}