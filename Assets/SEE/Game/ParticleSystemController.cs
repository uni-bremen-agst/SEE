using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game
{
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleSystemController : MonoBehaviour
    {
        [SerializeField] private ParticleSystem system;
        [SerializeField] private Transform srcTransform;
        [SerializeField] private Transform dstTransform;

        void Start()
        {
            SetPositions(srcTransform.position, dstTransform.position);
        }

        void Update()
        {
        }

        public void SetPositions(Vector3 src, Vector3 dst)
        {
            Assert.IsTrue(transform.localScale.x == transform.localScale.y && transform.localScale.y == transform.localScale.z);
            Assert.IsTrue(src != dst);

            Vector3 direction = dst - src;
            float distance = direction.magnitude;

            system.transform.forward = direction;
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(main.startSpeed.constant * transform.localScale.x * distance);

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 8.0f;
        }
    }
}
