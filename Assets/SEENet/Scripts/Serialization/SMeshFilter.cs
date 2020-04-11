using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net.Internal
{

    [Serializable]
    public class SMeshFilter : IInitializer<MeshFilter>
    {
        public SMesh sharedMesh;
        public SMesh mesh;

        public SMeshFilter(MeshFilter mf)
        {
            Assert.IsNotNull(mf);

            sharedMesh = mf.sharedMesh == null ? null : new SMesh(mf.sharedMesh);
            mesh = mf.mesh == null ? null : new SMesh(mf.mesh);
        }

        public void Initialize(MeshFilter value)
        {
            Assert.IsNotNull(value);

            if (value.sharedMesh == null)
            {
                value.sharedMesh = new Mesh();
            }
            sharedMesh.Initialize(value.sharedMesh);

            if (value.mesh == null)
            {
                value.mesh = new Mesh();
            }
            mesh.Initialize(value.mesh);
        }
    }

}
