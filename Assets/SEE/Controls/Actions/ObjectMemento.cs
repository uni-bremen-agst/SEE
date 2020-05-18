using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// A memento for the state of a game object to restore its location and position.
    /// </summary>
    internal struct ObjectMemento
    {
        public ObjectMemento(GameObject go)
        {
            this.go = go;
            this.position = go.transform.position;
            this.localScale = go.transform.localScale;
        }
        private readonly GameObject go;
        private Vector3 position;
        private Vector3 localScale;

        public void Reset()
        {
            go.transform.position = position;
            go.transform.localScale = localScale;

        }
        public GameObject Node
        {
            get => go;
        }
        /// <summary>
        /// Original world space position.
        /// </summary>
        public Vector3 Position
        {
            get => position;
        }
        /// <summary>
        /// Original world space scale (lossy scale).
        /// </summary>
        public Vector3 LocalScale
        {
            get => localScale;
        }

        public override string ToString()
        {
            return go.name
                + " position=" + position
                + " localScale=" + localScale;
        }
    }
}