using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace SEE.Net.Internal
{

    [Serializable]
    public class SMesh : IInitializer<Mesh>
    {
        [Serializable]
        public class SIndices
        {
            public int[] indices;
        }

        public string name;
        public IndexFormat indexFormat;
        public Matrix4x4[] bindposes;
        public int subMeshCount;
        public Bounds bounds;
        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector4[] tangents;
        public Vector2[] uv;
        public Vector2[] uv2;
        public Vector2[] uv3;
        public Vector2[] uv4;
        public Vector2[] uv5;
        public Vector2[] uv6;
        public Vector2[] uv7;
        public Vector2[] uv8;
        public Color[] colors;

        public SIndices[] indices;
        public MeshTopology[] topologies;
        public byte[] bonesPerVertex;
        public BoneWeight1[] boneWeights;

        public SMesh(Mesh m)
        {
            name = m.name;
            indexFormat = m.indexFormat;
            bindposes = m.bindposes;
            subMeshCount = m.subMeshCount;
            bounds = m.bounds;
            vertices = m.vertices;
            normals = m.normals;
            tangents = m.tangents;
            uv = m.uv;
            uv2 = m.uv2;
            uv3 = m.uv3;
            uv4 = m.uv4;
            uv5 = m.uv5;
            uv6 = m.uv6;
            uv7 = m.uv7;
            uv8 = m.uv8;

            indices = new SIndices[m.subMeshCount];
            topologies = new MeshTopology[m.subMeshCount];
            for (int i = 0; i < m.subMeshCount; i++)
            {
                indices[i] = new SIndices()
                {
                    indices = m.GetIndices(i)
                };
                topologies[i] = m.GetTopology(i);
            }
            bonesPerVertex = m.GetBonesPerVertex().ToArray();
            boneWeights = m.GetAllBoneWeights().ToArray();
        }

        public void Initialize(Mesh value)
        {
            value.name = name;
            value.indexFormat = indexFormat;
            value.bindposes = bindposes;
            value.subMeshCount = subMeshCount;
            value.bounds = bounds;
            value.vertices = vertices;
            value.normals = normals;
            value.tangents = tangents;
            value.uv = uv;
            value.uv2 = uv2;
            value.uv3 = uv3;
            value.uv4 = uv4;
            value.uv5 = uv5;
            value.uv6 = uv6;
            value.uv7 = uv7;
            value.uv8 = uv8;

            for (int i = 0; i < subMeshCount; i++)
            {
                value.SetIndices(indices[i].indices, topologies[i], i);
            }
            value.SetBoneWeights(
                new NativeArray<byte>(bonesPerVertex, Allocator.Temp),
                new NativeArray<BoneWeight1>(boneWeights, Allocator.Temp)
            );
        }
    }

}
