using SEE.Game;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Interact
{

    public class LoadCityInteraction : AbstractInteraction
    {
        public readonly AbstractSEECity city;
        public readonly Type type;

        public LoadCityInteraction(AbstractSEECity city)
        {
            type = city.GetType();
            this.city = city;
        }

        internal override void ExecuteLocally()
        {
            if (type == typeof(SEECity))
            {
                ((SEECity)city).LoadAndDrawGraph();
            }
            else if (type == typeof(SEEDynCity))
            {
                ((SEEDynCity)city).LoadAndDrawGraph();
            }
            else if (type == typeof(SEECityRandom))
            {
                ((SEECityRandom)city).LoadAndDrawGraph();
            }
            else if (type == typeof(SEECityEvolution))
            {
                Debug.LogWarning("This is not implemented!");
            }
            else
            {
                Debug.LogError("Unknown city-type!");
            }
        }
    }

    internal static class LoadCityInteractionSerializer
    {
        private struct SerializedObject
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
        }

        internal static string Serialize(LoadCityInteraction interaction)
        {
            SerializedObject serializedObject = new SerializedObject()
            {
                type = interaction.type.ToString(),
                pathPrefix = interaction.city.PathPrefix,
                origin = interaction.city.origin,
                innerDonutMetric = interaction.city.InnerDonutMetric,
                innerNodeStyleMetric = interaction.city.InnerNodeStyleMetric,
                minimalBlockLength = interaction.city.MinimalBlockLength,
                maximalBlockLength = interaction.city.MaximalBlockLength,
                leafObjects = interaction.city.LeafObjects,
                innerNodeObjects = interaction.city.InnerNodeObjects,
                nodeLayout = interaction.city.NodeLayout,
                edgeLayout = interaction.city.EdgeLayout,
                zScoreScale = interaction.city.ZScoreScale,
                edgeWidth = interaction.city.EdgeWidth,
                showErosions = interaction.city.ShowErosions,
                edgesAboveBlocks = interaction.city.EdgesAboveBlocks,
                tension = interaction.city.Tension
            };
            if (interaction.type == typeof(SEECity))
            {
                serializedObject.gxlPath = ((SEECity)interaction.city).gxlPath;
                serializedObject.csvPath = ((SEECity)interaction.city).csvPath;
            }
            if (interaction.type == typeof(SEECityEvolution))
            {
                serializedObject.maxRevisionsToLoad = ((SEECityEvolution)interaction.city).maxRevisionsToLoad;
            }
            if (interaction.type == typeof(SEEDynCity))
            {
                serializedObject.dynPath = ((SEEDynCity)interaction.city).dynPath;
            }
            if (interaction.type == typeof(SEECityRandom))
            {
                serializedObject.leafConstraint = ((SEECityRandom)interaction.city).LeafConstraint;
                serializedObject.innerNodeConstraint = ((SEECityRandom)interaction.city).InnerNodeConstraint;
            }
            string result = JsonUtility.ToJson(serializedObject);
            return result;
        }

        internal static LoadCityInteraction Deserialize(string interaction)
        {
            SerializedObject serializedObject = JsonUtility.FromJson<SerializedObject>(interaction);
            GameObject go = new GameObject(serializedObject.type);
            AbstractSEECity city = null;
            Type type = Type.GetType(serializedObject.type);
            if (type == typeof(SEECity))
            {
                city = go.AddComponent<SEECity>();
                ((SEECity)city).gxlPath = serializedObject.gxlPath;
                ((SEECity)city).csvPath = serializedObject.csvPath;
            }
            if (type == typeof(SEECityEvolution))
            {
                city = go.AddComponent<SEECityEvolution>();
                ((SEECityEvolution)city).maxRevisionsToLoad = serializedObject.maxRevisionsToLoad;
            }
            if (type == typeof(SEEDynCity))
            {
                city = go.AddComponent<SEEDynCity>();
                ((SEEDynCity)city).dynPath = serializedObject.dynPath;
            }
            if (type == typeof(SEECityRandom))
            {
                city = go.AddComponent<SEECityRandom>();
                ((SEECityRandom)city).LeafConstraint = serializedObject.leafConstraint;
                ((SEECityRandom)city).InnerNodeConstraint = serializedObject.innerNodeConstraint;
            }
            Assert.IsNotNull(city);

            city.PathPrefix = serializedObject.pathPrefix;
            city.origin = serializedObject.origin;
            city.InnerDonutMetric = serializedObject.innerDonutMetric;
            city.InnerNodeStyleMetric = serializedObject.innerNodeStyleMetric;
            city.MinimalBlockLength = serializedObject.minimalBlockLength;
            city.MaximalBlockLength = serializedObject.maximalBlockLength;
            city.LeafObjects = serializedObject.leafObjects;
            city.InnerNodeObjects = serializedObject.innerNodeObjects;
            city.NodeLayout = serializedObject.nodeLayout;
            city.EdgeLayout = serializedObject.edgeLayout;
            city.ZScoreScale = serializedObject.zScoreScale;
            city.EdgeWidth = serializedObject.edgeWidth;
            city.ShowErosions = serializedObject.showErosions;
            city.EdgesAboveBlocks = serializedObject.edgesAboveBlocks;
            city.Tension = serializedObject.tension;

            LoadCityInteraction result = new LoadCityInteraction(city);
            return result;
        }
    }

}