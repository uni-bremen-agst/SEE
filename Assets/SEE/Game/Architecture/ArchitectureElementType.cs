using System;
using UnityEngine;

namespace SEE.Game.Architecture
{
    /// <summary>
    /// The kinds of architecture elements available.
    /// </summary>
    public enum ArchitectureElementType : byte
    {
        Cluster,
        Component,
        Count
    }

    
    /// <summary>
    /// The node settings for a specific kind of <see cref="ArchitectureElementType"/>.
    /// </summary>
    public class ArchitectureElementSettings
    {
        /// <summary>
        /// The type of this architectural node.
        /// </summary>
        public ArchitectureElementType ElementType;

        /// <summary>
        /// The global height for this type of architectural node.
        /// </summary>
        public float ElementHeight = 0.01f;
        
        /// <summary>
        /// 
        /// </summary>
        public ColorRange ColorRange = new ColorRange(Color.white, Color.red, 10);

    }
}