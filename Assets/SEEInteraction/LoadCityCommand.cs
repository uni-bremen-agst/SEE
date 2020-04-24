using SEE.Game;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Command
{

    public class LoadCityCommand : AbstractCommand
    {
        public string type;

        // AbstractSEECity
        public string pathPrefix;
        public Vector3 origin;
        public string innerDonutMetric;
        public string innerNodeStyleMetric;
        public float minimalBlockLength;
        public float maximalBlockLength;
        public AbstractSEECity.LeafNodeKinds leafObjects;
        public AbstractSEECity.InnerNodeKinds innerNodeObjects;
        public AbstractSEECity.NodeLayouts nodeLayout;
        public AbstractSEECity.EdgeLayouts edgeLayout;
        public bool zScoreScale;
        public float edgeWidth;
        public bool showErosions;
        public bool edgesAboveBlocks;
        public float tension;

        // SEECity
        public string gxlPath;
        public string csvPath;

        // SEECityEvolution
        public int maxRevisionsToLoad;

        // SEECityDyn
        public string dynPath;

        // SEECityRandom
        public Tools.Constraint leafConstraint;
        public Tools.Constraint innerNodeConstraint;

        public LoadCityCommand(AbstractSEECity city)
        {
            type = city.GetType().ToString();

            pathPrefix = city.PathPrefix;
            origin = city.origin;
            innerDonutMetric = city.InnerDonutMetric;
            innerNodeStyleMetric = city.InnerNodeStyleMetric;
            minimalBlockLength = city.MinimalBlockLength;
            maximalBlockLength = city.MaximalBlockLength;
            leafObjects = city.LeafObjects;
            innerNodeObjects = city.InnerNodeObjects;
            nodeLayout = city.NodeLayout;
            edgeLayout = city.EdgeLayout;
            zScoreScale = city.ZScoreScale;
            edgeWidth = city.EdgeWidth;
            showErosions = city.ShowErosions;
            edgesAboveBlocks = city.EdgesAboveBlocks;
            tension = city.Tension;

            if (city.GetType() == typeof(SEECity))
            {
                gxlPath = ((SEECity)city).gxlPath;
                csvPath = ((SEECity)city).csvPath;
            }

            if (city.GetType() == typeof(SEECityEvolution))
            {
                maxRevisionsToLoad = ((SEECityEvolution)city).maxRevisionsToLoad;
            }

            if (city.GetType() == typeof(SEEDynCity))
            {
                dynPath = ((SEEDynCity)city).dynPath;
            }

            if (city.GetType() == typeof(SEECityRandom))
            {
                leafConstraint = ((SEECityRandom)city).LeafConstraint;
                innerNodeConstraint = ((SEECityRandom)city).InnerNodeConstraint;
            }
        }

        internal override void ExecuteLocally()
        {
            GameObject gameObject = new GameObject(type.ToString());
            AbstractSEECity city = null;

            Type t = Type.GetType(type);
            if (t == typeof(SEECity))
            {
                city = gameObject.AddComponent<SEECity>();
                ((SEECity)city).gxlPath = gxlPath;
                ((SEECity)city).csvPath = csvPath;
            }

            if (t == typeof(SEECityEvolution))
            {
                city = gameObject.AddComponent<SEECityEvolution>();
                ((SEECityEvolution)city).maxRevisionsToLoad = maxRevisionsToLoad;
            }

            if (t == typeof(SEEDynCity))
            {
                city = gameObject.AddComponent<SEEDynCity>();
                ((SEEDynCity)city).dynPath = dynPath;
            }

            if (t == typeof(SEECityRandom))
            {
                city = gameObject.AddComponent<SEECityRandom>();
                ((SEECityRandom)city).LeafConstraint = leafConstraint;
                ((SEECityRandom)city).InnerNodeConstraint = innerNodeConstraint;
            }

            Assert.IsNotNull(city);

            city.PathPrefix = pathPrefix;
            city.origin = origin;
            city.InnerDonutMetric = innerDonutMetric;
            city.InnerNodeStyleMetric = innerNodeStyleMetric;
            city.MinimalBlockLength = minimalBlockLength;
            city.MaximalBlockLength = maximalBlockLength;
            city.LeafObjects = leafObjects;
            city.InnerNodeObjects = innerNodeObjects;
            city.NodeLayout = nodeLayout;
            city.EdgeLayout = edgeLayout;
            city.ZScoreScale = zScoreScale;
            city.EdgeWidth = edgeWidth;
            city.ShowErosions = showErosions;
            city.EdgesAboveBlocks = edgesAboveBlocks;
            city.Tension = tension;

            if (t == typeof(SEECity))
            {
                ((SEECity)city).LoadAndDrawGraph();
            }
            else if (t == typeof(SEEDynCity))
            {
                ((SEEDynCity)city).LoadAndDrawGraph();
            }
            else if (t == typeof(SEECityRandom))
            {
                ((SEECityRandom)city).LoadAndDrawGraph();
            }
            else if (t == typeof(SEECityEvolution))
            {
                Debug.LogWarning("This is not implemented!");
            }
            else
            {
                Debug.LogError("Unknown city-type!");
            }
        }
    }

}