using SEE.GO;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Game
{
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleSystemController : MonoBehaviour
    {
        #region TEMP

        [SerializeField] private Transform srcTransform;
        [SerializeField] private Transform dstTransform;

        private void Start()
        {
            SetPositions(srcTransform.position, dstTransform.position);
            SEECity arch = SceneQueries.GetArch();
            SEECity impl = SceneQueries.GetImpl();
            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            //int renderQueueOffset = arch.LoadedGraph.MaxDepth > impl.LoadedGraph.MaxDepth ? arch.LoadedGraph.MaxDepth : impl.LoadedGraph.MaxDepth;
            renderer.material = Materials.New(Materials.MeshParticleSystemMaterialName, new Color(1.0f, 0.4f, 0.0f, 1.0f)/*, renderQueueOffset*/);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                BeginBurst(8.0f, 1.0f);
            }
        }

        #endregion

        [SerializeField] private ParticleSystem system;

        private float baseRate = 0.0f;

        public static ParticleSystemController Create(Vector3 src, Vector3 dst)
        {
            GameObject go = new GameObject("ParticleSystemController");
            ParticleSystemController psc = go.AddComponent<ParticleSystemController>();
            psc.SetPositions(src, dst);
            return psc;
        }

        public void SetPositions(Vector3 src, Vector3 dst)
        {
            Assert.IsTrue(transform.localScale.x == transform.localScale.y && transform.localScale.y == transform.localScale.z);
            Assert.IsTrue(src != dst);

            transform.position = src;

            Vector3 direction = dst - src;
            float distance = direction.magnitude;

            system.transform.forward = direction;
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(distance / (transform.localScale.x * main.startSpeed.constant));
        }

        public void BeginBurst(float rate, float seconds)
        {
            Assert.IsTrue(seconds > 0.0f && baseRate == 0.0f);

            ParticleSystem.EmissionModule emission = system.emission;
            baseRate = emission.rateOverTime.constant;
            Assert.IsTrue(baseRate < rate);
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(rate);

            Invoke("StopBurst", seconds);
        }

        public void StopBurst()
        {
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(baseRate);
            baseRate = 0.0f;
        }
    }
}
