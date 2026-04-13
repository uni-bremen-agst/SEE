using System.Collections.Generic;
using UnityEngine;

namespace SEE.Utils.Drawable
{
    public static class MeshValidationUtility
    {
        /// <summary>
        /// Entfernt Punkte, die zu nah beieinander liegen.
        /// Sehr wichtig für Netzwerkdaten!
        /// </summary>
        public static Vector3[] RemoveTooClosePoints(Vector3[] positions, float minDistance = 0.001f)
        {
            if (positions == null || positions.Length == 0)
                return positions;

            List<Vector3> filtered = new List<Vector3>();
            Vector3 last = positions[0];
            filtered.Add(last);

            for (int i = 1; i < positions.Length; i++)
            {
                if (Vector3.Distance(last, positions[i]) > minDistance)
                {
                    filtered.Add(positions[i]);
                    last = positions[i];
                }
            }

            return filtered.ToArray();
        }

        /// <summary>
        /// Prüft, ob ein Mesh für PhysX gültig ist.
        /// </summary>
        public static bool IsMeshValid(Mesh mesh, float minTriangleArea = 0.00001f)
        {
            if (mesh == null)
                return false;

            var vertices = mesh.vertices;
            var triangles = mesh.triangles;

            if (vertices == null || vertices.Length < 3)
                return false;

            if (triangles == null || triangles.Length < 3 || triangles.Length % 3 != 0)
                return false;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 b = vertices[triangles[i + 1]];
                Vector3 c = vertices[triangles[i + 2]];

                // NaN / Infinity Check
                if (!IsVectorValid(a) || !IsVectorValid(b) || !IsVectorValid(c))
                    return false;

                // Degenerierte Dreiecke (Fläche ~ 0)
                float area = Vector3.Cross(b - a, c - a).magnitude;
                if (area > minTriangleArea)
                    return true; // mindestens EIN gültiges Dreieck reicht
            }

            return false;
        }

        /// <summary>
        /// Prüft ob ein Vector gültig ist (keine NaN/Infinity)
        /// </summary>
        public static bool IsVectorValid(Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
                     float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
        }

        /// <summary>
        /// Baut ein Mesh aus einem LineRenderer und weist es sicher einem MeshCollider zu.
        /// </summary>
        public static bool TryAssignMeshToCollider(LineRenderer renderer, MeshCollider collider,
            float minDistance = 0.001f, float minTriangleArea = 0.00001f)
        {
            if (renderer == null || collider == null)
                return false;

            // 🔹 1. Positions holen & bereinigen
            Vector3[] positions = new Vector3[renderer.positionCount];
            renderer.GetPositions(positions);

            positions = RemoveTooClosePoints(positions, minDistance);

            if (positions.Length < 2)
                return false;

            renderer.positionCount = positions.Length;
            renderer.SetPositions(positions);

            // 🔹 2. Mesh erzeugen
            Mesh mesh = new Mesh();
            renderer.BakeMesh(mesh);

            // 🔹 3. Validieren
            if (!IsMeshValid(mesh, minTriangleArea))
                return false;

            // 🔹 4. Collider sicher setzen (wichtig!)
            collider.sharedMesh = null;
            collider.sharedMesh = mesh;

            return true;
        }
    }
}