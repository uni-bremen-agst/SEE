using SEE.Game;
using SEE.Layout.EdgeLayouts;
using SEE.Layout.NodeLayouts;
using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{

    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractAction"/> before modifying this class!
    ///   
    /// Loads a city with the attributes defined in object with name
    /// <see cref="gameObjectName"/> for every client.
    /// </summary>
    public class LoadCityAction : AbstractAction
    {
        /// <summary>
        /// The name of the game object defining the loading details.
        /// </summary>
        public string gameObjectName;

        /// <summary>
        /// The type of the city as string.
        /// </summary>
        public string type;

        /// <summary>
        /// The global position of the city.
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// The global rotation of the city.
        /// </summary>
        public Quaternion rotation;

        /// <summary>
        /// The global scale of the city.
        /// </summary>
        public Vector3 scale;

        //-----------------------------------------------------------------------
        // AbstractSEECity
        //-----------------------------------------------------------------------

        // TODO: HierarchicalEdges
        // TODO: nodeTypes
        public string WidthMetric;
        public string HeightMetric;
        public string DepthMetric;
        public string LeafStyleMetric;

        public string ArchitectureIssue;
        public string CloneIssue;
        public string CycleIssue;
        public string Dead_CodeIssue;
        public string MetricIssue;
        public string StyleIssue;
        public string UniversalIssue;

        public string ArchitectureIssue_SUM;
        public string CloneIssue_SUM;
        public string CycleIssue_SUM;
        public string Dead_CodeIssue_SUM;
        public string MetricIssue_SUM;
        public string StyleIssue_SUM;
        public string UniversalIssue_SUM;

        public string InnerDonutMetric;

        public string InnerNodeStyleMetric;

        public float MinimalBlockLength;
        public float MaximalBlockLength;

        public AbstractSEECity.LeafNodeKinds LeafObjects;
        public AbstractSEECity.InnerNodeKinds InnerNodeObjects;

        public NodeLayoutKind NodeLayout;
        public EdgeLayoutKind EdgeLayout;
        public bool ZScoreScale;
        public float EdgeWidth;
        public bool ShowErosions;
        public float MaxErosionWidth;
        public bool EdgesAboveBlocks;
        public float Tension;
        public float RDP;

        public int TubularSegments;
        public float Radius;
        public int RadialSegments;
        public bool isEdgeSelectable;

        //-----------------------------------------------------------------------
        // SEECity
        //-----------------------------------------------------------------------

        public DataPath gxlPath;
        public DataPath csvPath;

        //-----------------------------------------------------------------------
        // SEECityEvolution
        //-----------------------------------------------------------------------

        public int maxRevisionsToLoad;

        //-----------------------------------------------------------------------
        // SEECityDyn
        //-----------------------------------------------------------------------

        public DataPath dynPath;

        //-----------------------------------------------------------------------
        // SEEJlgCity
        //-----------------------------------------------------------------------

        public DataPath jlgPath;

        //-----------------------------------------------------------------------
        // SEECityRandom
        //-----------------------------------------------------------------------

        public Tools.Constraint leafConstraint;
        public Tools.Constraint innerNodeConstraint;

        /// <summary>
        /// Constructs a an action to load the given city for every client.
        /// </summary>
        /// <param name="city">The city to load.</param>
        public LoadCityAction(AbstractSEECity city)
        {
            gameObjectName = city.name;
            type = city.GetType().ToString();
            position = city.transform.position;
            rotation = city.transform.rotation;
            scale = city.transform.lossyScale;

            WidthMetric = city.WidthMetric;
            HeightMetric = city.HeightMetric;
            DepthMetric = city.DepthMetric;
            LeafStyleMetric = city.LeafStyleMetric;

            ArchitectureIssue = city.ArchitectureIssue;
            CloneIssue = city.CloneIssue;
            CycleIssue = city.CycleIssue;
            Dead_CodeIssue = city.Dead_CodeIssue;
            MetricIssue = city.MetricIssue;
            StyleIssue = city.StyleIssue;
            UniversalIssue = city.UniversalIssue;

            ArchitectureIssue_SUM = city.ArchitectureIssue_SUM;
            CloneIssue_SUM = city.CloneIssue_SUM;
            CycleIssue_SUM = city.CycleIssue_SUM;
            Dead_CodeIssue_SUM = city.Dead_CodeIssue_SUM;
            MetricIssue_SUM = city.MetricIssue_SUM;
            StyleIssue_SUM = city.StyleIssue_SUM;
            UniversalIssue_SUM = city.UniversalIssue_SUM;

            InnerDonutMetric = city.InnerDonutMetric;

            InnerNodeStyleMetric = city.InnerNodeStyleMetric;

            MinimalBlockLength = city.MinimalBlockLength;
            MaximalBlockLength = city.MaximalBlockLength;

            LeafObjects = city.LeafObjects;
            InnerNodeObjects = city.InnerNodeObjects;

            NodeLayout = city.NodeLayout;
            EdgeLayout = city.EdgeLayout;
            ZScoreScale = city.ZScoreScale;
            EdgeWidth = city.EdgeWidth;
            ShowErosions = city.ShowErosions;
            MaxErosionWidth = city.MaxErosionWidth;
            EdgesAboveBlocks = city.EdgesAboveBlocks;
            Tension = city.Tension;
            RDP = city.RDP;

            TubularSegments = city.TubularSegments;
            Radius = city.Radius;
            RadialSegments = city.RadialSegments;
            isEdgeSelectable = city.isEdgeSelectable;


            if (city.GetType() == typeof(SEECity))
            {
                gxlPath = ((SEECity)city).GXLPath;
                csvPath = ((SEECity)city).CSVPath;
            }

            if (city.GetType() == typeof(SEECityEvolution))
            {
                maxRevisionsToLoad = ((SEECityEvolution)city).MaxRevisionsToLoad;
            }

            if (city.GetType() == typeof(SEEDynCity))
            {
                dynPath = ((SEEDynCity)city).DYNPath;
            }

            if (city.GetType() == typeof(SEEJlgCity))
            {
                jlgPath = ((SEEJlgCity)city).JLGPath;
            }

            if (city.GetType() == typeof(SEECityRandom))
            {
                leafConstraint = ((SEECityRandom)city).LeafConstraint;
                innerNodeConstraint = ((SEECityRandom)city).InnerNodeConstraint;
            }
        }



        protected override void ExecuteOnServer()
        {
        }

        /// <summary>
        /// Loads the city of given attributes.
        /// </summary>
        protected override void ExecuteOnClient()
        {
            GameObject gameObject = GameObject.Find(gameObjectName);
            Assert.IsNotNull(gameObject);

            gameObject.transform.position = position;
            gameObject.transform.rotation = rotation;
            gameObject.transform.localScale = scale;

            AbstractSEECity city = null;
            Type t = Type.GetType(type);

            if (t == typeof(SEECity))
            {
                city = gameObject.GetComponent<SEECity>();
                ((SEECity)city).GXLPath = gxlPath;
                ((SEECity)city).CSVPath = csvPath;
            }

            if (t == typeof(SEECityEvolution))
            {
                city = gameObject.GetComponent<SEECityEvolution>();
                ((SEECityEvolution)city).MaxRevisionsToLoad = maxRevisionsToLoad;
            }

            if (t == typeof(SEEDynCity))
            {
                city = gameObject.GetComponent<SEEDynCity>();
                ((SEEDynCity)city).DYNPath = dynPath;
            }

            if (t == typeof(SEEJlgCity))
            {
                city = gameObject.GetComponent<SEEJlgCity>();
                ((SEEJlgCity)city).JLGPath = jlgPath;
            }

            if (t == typeof(SEECityRandom))
            {
                city = gameObject.GetComponent<SEECityRandom>();
                ((SEECityRandom)city).LeafConstraint = leafConstraint;
                ((SEECityRandom)city).InnerNodeConstraint = innerNodeConstraint;
            }

            Assert.IsNotNull(city);

            city.WidthMetric = WidthMetric;
            city.HeightMetric = HeightMetric;
            city.DepthMetric = DepthMetric;
            city.LeafStyleMetric = LeafStyleMetric;

            city.ArchitectureIssue = ArchitectureIssue;
            city.CloneIssue = CloneIssue;
            city.CycleIssue = CycleIssue;
            city.Dead_CodeIssue = Dead_CodeIssue;
            city.MetricIssue = MetricIssue;
            city.StyleIssue = StyleIssue;
            city.UniversalIssue = UniversalIssue;

            city.ArchitectureIssue_SUM = ArchitectureIssue_SUM;
            city.CloneIssue_SUM = CloneIssue_SUM;
            city.CycleIssue_SUM = CycleIssue_SUM;
            city.Dead_CodeIssue_SUM = Dead_CodeIssue_SUM;
            city.MetricIssue_SUM = MetricIssue_SUM;
            city.StyleIssue_SUM = StyleIssue_SUM;
            city.UniversalIssue_SUM = UniversalIssue_SUM;

            city.InnerDonutMetric = InnerDonutMetric;

            city.InnerNodeStyleMetric = InnerNodeStyleMetric;

            city.MinimalBlockLength = MinimalBlockLength;
            city.MaximalBlockLength = MaximalBlockLength;

            city.LeafObjects = LeafObjects;
            city.InnerNodeObjects = InnerNodeObjects;

            city.NodeLayout = NodeLayout;
            city.EdgeLayout = EdgeLayout;
            city.ZScoreScale = ZScoreScale;
            city.EdgeWidth = EdgeWidth;
            city.ShowErosions = ShowErosions;
            city.MaxErosionWidth = MaxErosionWidth;
            city.EdgesAboveBlocks = EdgesAboveBlocks;
            city.Tension = Tension;
            city.RDP = RDP;

            city.TubularSegments = TubularSegments;
            city.Radius = Radius;
            city.RadialSegments = RadialSegments;
            city.isEdgeSelectable = isEdgeSelectable;

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