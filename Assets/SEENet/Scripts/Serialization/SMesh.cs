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

        public SMesh(Mesh mesh)
        {
            indexFormat = mesh.indexFormat;
            bindposes = mesh.bindposes;
            subMeshCount = mesh.subMeshCount;
            bounds = mesh.bounds;
            vertices = mesh.vertices;
            normals = mesh.normals;
            tangents = mesh.tangents;
            uv = mesh.uv;
            uv2 = mesh.uv2;
            uv3 = mesh.uv3;
            uv4 = mesh.uv4;
            uv5 = mesh.uv5;
            uv6 = mesh.uv6;
            uv7 = mesh.uv7;
            uv8 = mesh.uv8;

            indices = new SIndices[mesh.subMeshCount];
            topologies = new MeshTopology[mesh.subMeshCount];
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                indices[i] = new SIndices()
                {
                    indices = mesh.GetIndices(i)
                };
                topologies[i] = mesh.GetTopology(i);
            }
            bonesPerVertex = mesh.GetBonesPerVertex().ToArray();
            boneWeights = mesh.GetAllBoneWeights().ToArray();
        }

        public void Initialize(Mesh value)
        {
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
