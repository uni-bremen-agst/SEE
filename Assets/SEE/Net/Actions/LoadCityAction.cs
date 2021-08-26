using System;
using SEE.Game;
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

        public LeafNodeKinds LeafObjects;
        public InnerNodeKinds InnerNodeObjects;

        public NodeLayoutKind NodeLayout;
        public EdgeLayoutKind EdgeLayout;
        public bool ZScoreScale;
        public bool ScaleOnlyLeafMetrics;

        public float EdgeWidth;
        public bool ShowLeafErosions;
        public bool ShowInnerErosions;
        public bool LoadDashboardMetrics;
        public string IssuesAddedFromVersion;
        public bool OverrideMetrics;
        public float ErosionScalingFactor;
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
            // FIXME if this class is still wanted, it needs to be fixed and tested. the city
            // settings had large refactorings and this was left out as of now.

            gameObjectName = city.name;
            type = city.GetType().ToString();
            position = city.transform.position;
            rotation = city.transform.rotation;
            scale = city.transform.lossyScale;

            //WidthMetric = city.leafNodeAttributes.widthMetric;
            //HeightMetric = city.leafNodeAttributes.heightMetric;
            //DepthMetric = city.leafNodeAttributes.depthMetric;
            //LeafStyleMetric = city.leafNodeAttributes.styleMetric;

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

            //InnerNodeStyleMetric = city.innerNodeAttributes.styleMetric;

            MinimalBlockLength = city.MinimalBlockLength;
            MaximalBlockLength = city.MaximalBlockLength;

            //LeafObjects = city.nodeLayoutSettings.leafKind;
            //InnerNodeObjects = city.nodeLayoutSettings.innerKind;

            NodeLayout = city.nodeLayoutSettings.kind;
            EdgeLayout = city.edgeLayoutSettings.kind;
            ZScoreScale = city.nodeLayoutSettings.zScoreScale;
            ScaleOnlyLeafMetrics = city.nodeLayoutSettings.ScaleOnlyLeafMetrics;

            EdgeWidth = city.edgeLayoutSettings.edgeWidth;
            ShowInnerErosions = city.nodeLayoutSettings.showInnerErosions;
            ShowLeafErosions = city.nodeLayoutSettings.showLeafErosions;
            LoadDashboardMetrics = city.nodeLayoutSettings.loadDashboardMetrics;
            IssuesAddedFromVersion = city.nodeLayoutSettings.issuesAddedFromVersion;
            OverrideMetrics = city.nodeLayoutSettings.overrideMetrics;
            ErosionScalingFactor = city.nodeLayoutSettings.erosionScalingFactor;
            EdgesAboveBlocks = city.edgeLayoutSettings.edgesAboveBlocks;
            Tension = city.edgeLayoutSettings.tension;
            RDP = city.edgeLayoutSettings.rdp;

            TubularSegments = city.edgeLayoutSettings.tubularSegments;
            Radius = city.edgeLayoutSettings.radius;
            RadialSegments = city.edgeLayoutSettings.radialSegments;
            isEdgeSelectable = city.edgeLayoutSettings.isEdgeSelectable;


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

            //city.leafNodeAttributes.widthMetric = WidthMetric;
            //city.leafNodeAttributes.heightMetric = HeightMetric;
            //city.leafNodeAttributes.depthMetric = DepthMetric;
            //city.leafNodeAttributes.styleMetric = LeafStyleMetric;

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

            //city.innerNodeAttributes.styleMetric = InnerNodeStyleMetric;

            city.MinimalBlockLength = MinimalBlockLength;
            city.MaximalBlockLength = MaximalBlockLength;

            //city.nodeLayoutSettings.leafKind = LeafObjects;
            //city.nodeLayoutSettings.innerKind = InnerNodeObjects;

            city.nodeLayoutSettings.kind = NodeLayout;
            city.edgeLayoutSettings.kind = EdgeLayout;
            city.nodeLayoutSettings.zScoreScale = ZScoreScale;
            city.nodeLayoutSettings.ScaleOnlyLeafMetrics = ScaleOnlyLeafMetrics;

            city.edgeLayoutSettings.edgeWidth = EdgeWidth;
            city.nodeLayoutSettings.showInnerErosions = ShowInnerErosions;
            city.nodeLayoutSettings.showLeafErosions = ShowLeafErosions;
            city.nodeLayoutSettings.loadDashboardMetrics = LoadDashboardMetrics;
            city.nodeLayoutSettings.issuesAddedFromVersion = IssuesAddedFromVersion;
            city.nodeLayoutSettings.overrideMetrics = OverrideMetrics;
            city.nodeLayoutSettings.erosionScalingFactor = ErosionScalingFactor;
            city.edgeLayoutSettings.edgesAboveBlocks = EdgesAboveBlocks;
            city.edgeLayoutSettings.tension = Tension;
            city.edgeLayoutSettings.rdp = RDP;

            city.edgeLayoutSettings.tubularSegments = TubularSegments;
            city.edgeLayoutSettings.radius = Radius;
            city.edgeLayoutSettings.radialSegments = RadialSegments;
            city.edgeLayoutSettings.isEdgeSelectable = isEdgeSelectable;

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