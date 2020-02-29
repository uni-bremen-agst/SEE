using System;
using UnityEngine;

namespace SEE.Net.Internal
{

    [Serializable]
    public class SMeshFilter : IInitializer<MeshFilter>
    {
        public SMesh sharedMesh;
        public SMesh mesh;

        public SMeshFilter(MeshFilter meshFilter)
        {
            sharedMesh = meshFilter.sharedMesh == null ? null : new SMesh(meshFilter.sharedMesh);
            mesh = meshFilter.mesh == null ? null : new SMesh(meshFilter.mesh);
        }

        public void Initialize(MeshFilter value)
        {
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
