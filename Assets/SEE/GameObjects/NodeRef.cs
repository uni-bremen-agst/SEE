using System;
using OdinSerializer;
using SEE.Charts.Scripts;
using SEE.DataModel;
using UnityEngine;
using UnityEngine.Serialization;

namespace SEE.GO
{
	/// <summary>
	/// A reference to a graph node that can be attached to a game object as a component.
	/// </summary>
	public class NodeRef : SerializedMonoBehaviour
	{
		/// <summary>
		/// The graph node this node reference is referring to. It will be set either
		/// by a graph renderer while in editor mode or at runtime by way of an
		/// AbstractSEECity object.
		/// It will not be serialized to prevent duplicating and endless serialization
		/// by both Unity and Odin.
		/// </summary>
		[NonSerialized] public Node node;

		[FormerlySerializedAs("Highlights"), HideInInspector]
		public NodeHighlights highlights;

		public void Awake()
		{
			highlights = GetComponent<NodeHighlights>();
		}
	}
}