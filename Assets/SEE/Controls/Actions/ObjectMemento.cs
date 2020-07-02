using UnityEngine;

namespace SEE.GO
{
    /// <summary>
    /// A memento for the state of a game object to restore its location and position.
    /// </summary>
    internal struct ObjectMemento
    {
        private readonly GameObject go;
        private Vector3 localPosition;
        private Vector3 localScale;

        public ObjectMemento(GameObject go)
        {
            this.go = go;
            localPosition = go.transform.localPosition;
            localScale = go.transform.localScale;
        }

        public void Reset()
        {
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

        }
        public GameObject Node
        {
            get => go;
        }
        /// <summary>
        /// Original world space position.
        /// </summary>
        public Vector3 LocalPosition
        {
            get => localPosition;
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
                + " localPosition=" + localPosition
                + " localScale=" + localScale
                + " worldSize=" + go.Size();
        }
    }
}