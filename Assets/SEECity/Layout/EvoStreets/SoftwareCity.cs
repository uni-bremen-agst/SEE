using System.Collections.Generic;
using UnityEngine;

namespace SEE.Layout.EvoStreets
{
    public class SoftwareCity
    {
        /// <summary>
        /// The leaf and inner nodes to be laid out.
        /// </summary>
        private List<GameObject> gameObjects = new List<GameObject>();

        /// <summary>
        /// Determines how to scale the node metrics.
        /// </summary>
        private IScale scaler;

        /// <summary>
        /// The settings to be considered for the layout.
        /// </summary>
        private GraphSettings graphSettings;

        private float groundLevel;

        private readonly NodeFactory leafNodeFactory;

        public SoftwareCity(float groundLevel, IScale scaler, NodeFactory leafNodeFactory, GraphSettings graphSettings)
        {
            this.groundLevel = groundLevel;
            this.graphSettings = graphSettings;
            this.scaler = scaler;
            this.leafNodeFactory = leafNodeFactory;
        }

        public Dictionary<GameObject, NodeTransform> Layout(ICollection<GameObject> gameObjects)
        {
            EvoStreetsNodeLayout evoStreetLayout = new EvoStreetsNodeLayout(groundLevel, leafNodeFactory, scaler, graphSettings);
            Dictionary<GameObject, NodeTransform> layout_result = evoStreetLayout.Layout(gameObjects);
            return layout_result;
        }
    }
}
