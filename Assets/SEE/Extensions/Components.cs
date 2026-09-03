using Sirenix.Utilities;
using System;
using UnityEngine;

namespace SEE.Extensions
{
    /// <summary>
    /// Provides extension methods to manage <see cref="UnityEngine.Component"/>s
    /// of <see cref="UnityEngine.GameObject"/>.
    /// </summary>
    public static class Components
    {
        /// <summary>
        /// Tries to get the component of the given type <typeparamref name="T"/> of this <paramref name="gameObject"/>.
        /// If the component was found, it will be stored in <paramref name="component"/> and true will be returned.
        /// If it wasn't found, <paramref name="component"/> will be null, false will be returned,
        /// and an error message will be logged indicating that the component type wasn't present on the GameObject.
        /// </summary>
        /// <param name="gameObject">The game object the component should be gotten from. Must not be null.</param>
        /// <param name="component">The variable in which to save the component.</param>
        /// <typeparam name="T">The type of the component.</typeparam>
        /// <returns>True if the component was present on the <paramref name="gameObject"/>, false otherwise.</returns>
        public static bool TryGetComponentOrLog<T>(this GameObject gameObject, out T component)
        {
            if (!gameObject.TryGetComponent(out component))
            {
                Debug.LogError($"Couldn't find component '{typeof(T).GetNiceName()}' "
                               + $"on game object '{gameObject.FullName()}'.\n");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to get the component of the given type <typeparamref name="T"/> of this <paramref name="gameObject"/>.
        /// If a component of the type was found, it will be returned, otherwise a new component of the type
        /// will be added and returned.
        /// </summary>
        /// <param name="gameObject">The gameobject whose component of type <typeparamref name="T"/>
        /// we wish to return.</param>
        /// <typeparam name="T">The component to get / add</typeparam>
        /// <returns>The existing or newly created component.</returns>
        public static T AddOrGetComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.TryGetComponent(out T component) ? component : gameObject.AddComponent<T>();
        }

        /// <summary>
        /// Tries to get the component of the given type <typeparamref name="T"/> of this <paramref name="gameObject"/>.
        /// If the component was found, it will be returned.
        /// If it wasn't found, <see cref="InvalidOperationException"/> will be thrown.
        /// </summary>
        /// <param name="gameObject">The game object the component should be gotten from. Must not be null.</param>
        /// <typeparam name="T">The type of the component.</typeparam>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="gameObject"/> has no
        /// component of type <typeparamref name="T"/>.</exception>
        public static T MustGetComponent<T>(this GameObject gameObject)
        {
            if (!gameObject.TryGetComponent(out T component))
            {
                throw new InvalidOperationException($"Couldn't find component '{typeof(T).GetNiceName()}' on game object '{gameObject.FullName()}'");
            }
            return component;
        }

    }
}
