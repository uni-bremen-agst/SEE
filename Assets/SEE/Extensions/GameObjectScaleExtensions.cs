using SEE.Utils;
using System;
using UnityEngine;

namespace SEE.Extensions
{
    /// <summary>
    /// Provides extensions for <see cref="GameObject"/>s regarding size (scale).
    /// </summary>
    public static class GameObjectScaleExtensions
    {
        /// <summary>
        /// Provides the size and the mesh offset of the given <paramref name="gameObject"/> in world space.
        /// This does not include the size of descendants if there are any.
        /// <para>
        /// This value reflects the actual world-space bounds of the axis-aligned cuboid that contains the rendered
        /// object.
        /// Please note that the <see cref="Transform.lossyScale"/> is only the scale factor and not the actual size of
        /// a rendered object.
        /// Similarly, <see cref="Transform.position"/> is not necessarily the center point of the rendered object.
        /// </para><para>
        /// <list type="bullet">
        /// <item>
        /// If a <see cref="Collider"/> is attached, the <see cref="Collider.bounds"/> will be used.
        /// </item><item>
        /// If a <see cref="LineRenderer"/> is attached, the bounds will be calculated based on its positions with a
        /// performance penalty (see <see cref="GeometryUtils.CalculateLineBounds"/>).
        /// </item><item>
        /// If a <see cref="Renderer"/> is attached, the <see cref="Renderer.bounds"/> will be used.
        /// </item><item>
        /// Else, <see cref="Transform.lossyScale"/> and <see cref="Transform.position"/> are provided and a warning
        /// is logged.
        /// It means that either the object is not rendered at all or this method needs to be extended.
        /// </item>
        /// </list>
        /// </para><para>
        /// Local-space counterpart: <see cref="LocalSize(GameObject, out Vector3, out Vector3)"/>
        /// </para>
        /// </summary>
        /// <param name="gameObject">Object whose scale is requested.</param>
        /// <param name="position">Out parameter for the world-space position of the object.</param>
        /// <param name="size">Out parameter for the world-space size of the object.</param>
        /// <returns>True if the size was successfully retrieved, false if the fallback was used.</returns>
        /// <remarks>Applicable to different kinds of game objects (with <see cref="Collider"/>,
        /// <see cref="LineRenderer"/>, general <see cref="Renderer"/>, or none of these).</remarks>
        public static bool WorldSpaceSize(this GameObject gameObject, out Vector3 size, out Vector3 position)
        {
            // Rely on collider bounds if available.
            if (gameObject.TryGetComponent(out Collider collider))
            {
                size = collider.bounds.size;
                position = collider.bounds.center;
                return true;
            }

            // For objects with a LineRenderer, we can use its positions to determine its bounds.
            // Otherwise Unity will return overly large bounds.
            if (gameObject.TryGetComponent(out LineRenderer lineRenderer))
            {
                Bounds lineBounds = GeometryUtils.CalculateLineBounds(lineRenderer, true);
                size = lineBounds.size;
                position = lineBounds.center;
                return true;
            }

            // For some objects, such as capsules or custom meshes, lossyScale gives wrong results.
            // The more reliable option to determine the size is using the
            // object's renderer if it has one.
            if (gameObject.TryGetComponent(out Renderer renderer))
            {
                size = renderer.bounds.size;
                position = renderer.bounds.center;
                return true;
            }

            // No renderer, so we use lossyScale as a fallback.
            // Note: This may happen for container objects that have no mesh.
            size = gameObject.transform.lossyScale;
            position = gameObject.transform.position;
            return false;
        }

        /// <summary>
        /// Returns the size of the given <paramref name="gameObject"/> in world space.
        /// This does not include the size of descendants if there are any.
        /// <para>
        /// This is a shorthand method for <see cref="WorldSpaceSize(GameObject, out Vector3, out Vector3)"/> that only returns the size.
        /// See there for additional documentation.
        /// </para><para>
        /// Use <see cref="WorldSpaceSize(GameObject, out Vector3, out Vector3)"/> directly if you need both position and size.
        /// </para><para>
        /// Local-space counterpart: <see cref="LocalSize(GameObject)"/>
        /// </para>
        /// </summary>
        /// <param name="gameObject">Object whose size is requested.</param>
        /// <returns>Size of given <paramref name="gameObject"/>.</returns>
        public static Vector3 WorldSpaceSize(this GameObject gameObject)
        {
            WorldSpaceSize(gameObject, out Vector3 size, out Vector3 _);
            return size;
        }

        /// <summary>
        /// Provides the size and the mesh offset of the given <paramref name="gameObject"/> in local space,
        /// i.e., in relation to its parent.
        /// This does not include the size of descendants if there are any.
        /// <para>
        /// This value should often be used instead of the <see cref="Transform.localScale"/> because the scale only
        /// reflects the size for objects with a standardized size like cube primitives. Similarly, the
        /// <see cref="Transform.localPosition"/> can be significantly off the object's center.
        /// </para><para>
        /// <list type="bullet">
        /// <item>
        /// If a <see cref="Collider"/> is attached, the <see cref="Collider.bounds"/> will be used and converted into
        /// local space.
        /// </item><item>
        /// If a <see cref="LineRenderer"/> is attached, the bounds will be calculated based on its positions with a
        /// performance penalty (see <see cref="GeometryUtils.CalculateLineBounds"/>).
        /// </item><item>
        /// If a <see cref="MeshFilter"/> is attached, the <see cref="MeshFilter.sharedMesh.bounds"/> will be used.
        /// </item><item>
        /// Else, <see cref="Transform.localScale"/> and <see cref="Transform.localPosition"/> are provided and a
        /// warning is logged.
        /// It means that either the object is not rendered at all or this method needs to be extended.
        /// </item>
        /// </list>
        /// </para><para>
        /// World-space counterpart: <see cref="WorldSpaceSize(GameObject, out Vector3, out Vector3)"/>
        /// </para>
        /// </summary>
        /// <param name="gameObject">Object whose scale is requested.</param>
        /// <param name="size">Out parameter for the local size of the object.</param>
        /// <param name="position">Out parameter for the local position of the object.</param>
        /// <returns>True if the <paramref name="gameObject"/> has a size, false if the fallback was used.</returns>
        /// <remarks>Applicable to game objects having a <see cref="Mesh"/> or <see cref="LineRenderer"/>
        /// and others (in the latter case, the localScale is used as a fallback.</remarks>
        public static bool LocalSize(this GameObject gameObject, out Vector3 size, out Vector3 position)
        {
            // Rely on collider bounds if available.
            if (gameObject.TryGetComponent(out Collider collider))
            {
                size = getLocalColliderSize(collider);
                position = collider.transform.InverseTransformPoint(collider.bounds.center) + gameObject.transform.localPosition;
                return true;
            }

            // For objects with a LineRenderer, we can use its positions to determine its bounds.
            // Otherwise Unity will return overly large bounds.
            if (gameObject.TryGetComponent(out LineRenderer lineRenderer))
            {
                Bounds lineBounds = GeometryUtils.CalculateLineBounds(lineRenderer, false);
                size = lineBounds.size;
                position = lineBounds.center;
                return true;
            }

            // For some objects, such as capsules or custom meshes, localScale gives wrong results.
            // The more reliable option to determine the size is using the object's mesh if it has one.
            Mesh sharedMesh;
            if (gameObject.TryGetComponent(out MeshFilter meshFilter) && (sharedMesh = meshFilter.sharedMesh) != null)
            {
                size = Vector3.Scale(sharedMesh.bounds.size, gameObject.transform.localScale);
                position = sharedMesh.bounds.center + gameObject.transform.localPosition;
                return true;
            }

            // No mesh, so we use localScale as a fallback.
            // Note: This should not happen. If the object has no mesh, it has no size at all.
            Debug.LogWarning($"GameObject {gameObject.FullName()} has neither a {nameof(Mesh)} nor {nameof(LineRenderer)}, "
                + "using localScale as fallback.\n");
            size = gameObject.transform.localScale;
            position = gameObject.transform.localPosition;
            return false;

            Vector3 getLocalColliderSize(Collider collider)
            {
                Vector3 localScale = collider.transform.localScale;

                if (collider is BoxCollider box)
                {
                    return Vector3.Scale(box.size, localScale);
                }
                else if (collider is SphereCollider sphere)
                {
                    float diameter = sphere.radius * 2f;
                    // Sphere scales uniformly in all axes
                    return new Vector3(diameter, diameter, diameter) * Mathf.Max(localScale.x, Mathf.Max(localScale.y, localScale.z));
                }
                else if (collider is CapsuleCollider capsule)
                {
                    float diameter = capsule.radius * 2f;
                    Vector3 size = Vector3.zero;
                    switch (capsule.direction)
                    {
                        case 0: // X axis
                            size = new Vector3(capsule.height, diameter, diameter);
                            break;
                        case 1: // Y axis
                            size = new Vector3(diameter, capsule.height, diameter);
                            break;
                        case 2: // Z axis
                            size = new Vector3(diameter, diameter, capsule.height);
                            break;
                        default:
                            // This should never happen
                            throw new NotImplementedException();
                    }
                    size.x *= localScale.x;
                    size.y *= localScale.y;
                    size.z *= localScale.z;
                    return size;
                }
                else if (collider is MeshCollider meshCollider)
                {
                    Mesh mesh = meshCollider.sharedMesh;
                    if (mesh != null)
                    {
                        return Vector3.Scale(mesh.bounds.size, localScale);
                    }
                    else
                    {
                        return Vector3.zero;
                    }
                }
                else
                {
                    // Fallback: bounds.size is in world space, convert to local by dividing by scale
                    Debug.LogWarning($"GameObject has unknown collider type, using localScale as fallback: {gameObject.name}");
                    Bounds worldBounds = collider.bounds;
                    Vector3 worldSize = worldBounds.size;
                    return new Vector3(
                        localScale.x != 0 ? worldSize.x / localScale.x : 0,
                        localScale.y != 0 ? worldSize.y / localScale.y : 0,
                        localScale.z != 0 ? worldSize.z / localScale.z : 0);
                }
            }
        }

        /// <summary>
        /// Returns the size of the given <paramref name="gameObject"/> in local space,
        /// i.e., in relation to its parent.
        /// <para>
        /// This is a shorthand method for <see cref="LocalSize(GameObject, out Vector3, out Vector3)"/> that only returns the size.
        /// See there for additional documentation.
        /// </para><para>
        /// Use <see cref="LocalSize(GameObject, out Vector3, out Vector3)"/> directly if you need both position and size.
        /// </para><para>
        /// World-space counterpart: <see cref="WorldSpaceSize(GameObject)"/>
        /// </para>
        /// </summary>
        /// <param name="gameObject">Object whose size is requested.</param>
        /// <returns>Size of given <paramref name="gameObject"/>.</returns>
        /// <remarks>Applicable to game objects having a <see cref="Mesh"/> or <see cref="LineRenderer"/>
        /// and others (in the latter case, the localScale is used as a fallback.</remarks>
        public static Vector3 LocalSize(this GameObject gameObject)
        {
            LocalSize(gameObject, out Vector3 size, out Vector3 _);
            return size;
        }

        /// <summary>
        /// Returns the bounds of the given <paramref name="gameObject"/> in its own
        /// local coordinate system.
        /// <para>
        /// Note: A primitive cube has a size of (1,1,1), and a coordinate center (pivot)
        /// of (0,0,0).
        /// However, that does not apply for all primitives or models in general.
        /// </para>
        /// </summary>
        /// <param name="gameObject">The game object.</param>
        /// <returns>Local-space bounds of <paramref name="gameObject"/>.</returns>
        /// <remarks>Applicable to game objects having a <see cref="Mesh"/> <see cref="LineRenderer"/>,
        /// a <see cref="Renderer"/> and others (in the latter case, the (Vector2.zero, Vector3.one)
        /// is used as a fallback.</remarks>
        public static Bounds LocalBounds(this GameObject gameObject)
        {
            // For objects with a LineRenderer, we can use its positions to determine its bounds.
            // Otherwise Unity will return overly large bounds.
            if (gameObject.TryGetComponent(out LineRenderer lineRenderer))
            {
                return GeometryUtils.CalculateLineBounds(lineRenderer, false);
            }
            if (gameObject.TryGetComponent(out MeshFilter meshFilter))
            {
                return meshFilter.sharedMesh.bounds;
            }
            if (gameObject.TryGetComponent(out Renderer renderer))
            {
                return new(
                        gameObject.transform.InverseTransformPoint(renderer.bounds.center),
                        gameObject.transform.InverseTransformVector(renderer.bounds.size));
            }
            // This fallback works for uniform primitives like cubes, but not for non-uniforms like cylinders.
            return new(Vector3.zero, Vector3.one);
        }
    }
}
