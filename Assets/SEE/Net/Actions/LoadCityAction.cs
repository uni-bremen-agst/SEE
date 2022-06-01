using System;
using SEE.Game.City;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace SEE.Net
{
    /// <summary>
    /// !!! IMPORTANT !!!
    ///   See <see cref="AbstractNetAction"/> before modifying this class!
    ///
    /// Loads a city with the attributes defined in object with name
    /// <see cref="gameObjectName"/> for every client.
    /// </summary>
    public class LoadCityAction : AbstractNetAction
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

        public NodeShapes LeafObjects;
        public NodeShapes InnerNodeObjects;

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

        public FilePath gxlPath;
        public FilePath csvPath;

        //-----------------------------------------------------------------------
        // SEECityEvolution
        //-----------------------------------------------------------------------

        public int maxRevisionsToLoad;

        //-----------------------------------------------------------------------
        // SEECityDyn
        //-----------------------------------------------------------------------

        public FilePath dynPath;

        //-----------------------------------------------------------------------
        // SEEJlgCity
        //-----------------------------------------------------------------------

        public FilePath jlgPath;

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

            ArchitectureIssue = city.ErosionSettings.ArchitectureIssue;
            CloneIssue = city.ErosionSettings.CloneIssue;
            CycleIssue = city.ErosionSettings.CycleIssue;
            Dead_CodeIssue = city.ErosionSettings.Dead_CodeIssue;
            MetricIssue = city.ErosionSettings.MetricIssue;
            StyleIssue = city.ErosionSettings.StyleIssue;
            UniversalIssue = city.ErosionSettings.UniversalIssue;

            ArchitectureIssue_SUM = city.ErosionSettings.ArchitectureIssue_SUM;
            CloneIssue_SUM = city.ErosionSettings.CloneIssue_SUM;
            CycleIssue_SUM = city.ErosionSettings.CycleIssue_SUM;
            Dead_CodeIssue_SUM = city.ErosionSettings.Dead_CodeIssue_SUM;
            MetricIssue_SUM = city.ErosionSettings.MetricIssue_SUM;
            StyleIssue_SUM = city.ErosionSettings.StyleIssue_SUM;
            UniversalIssue_SUM = city.ErosionSettings.UniversalIssue_SUM;

            //InnerNodeStyleMetric = city.innerNodeAttributes.styleMetric;

            MinimalBlockLength = city.LeafNodeSettings.MinimalBlockLength;
            MaximalBlockLength = city.LeafNodeSettings.MaximalBlockLength;

            //LeafObjects = city.nodeLayoutSettings.leafKind;
            //InnerNodeObjects = city.nodeLayoutSettings.innerKind;

            NodeLayout = city.NodeLayoutSettings.Kind;
            EdgeLayout = city.EdgeLayoutSettings.Kind;
            ZScoreScale = city.ZScoreScale;
            ScaleOnlyLeafMetrics = city.ScaleOnlyLeafMetrics;

            EdgeWidth = city.EdgeLayoutSettings.EdgeWidth;
            ShowInnerErosions = city.ErosionSettings.ShowInnerErosions;
            ShowLeafErosions = city.ErosionSettings.ShowLeafErosions;
            LoadDashboardMetrics = city.ErosionSettings.LoadDashboardMetrics;
            IssuesAddedFromVersion = city.ErosionSettings.IssuesAddedFromVersion;
            OverrideMetrics = city.ErosionSettings.OverrideMetrics;
            ErosionScalingFactor = city.ErosionSettings.ErosionScalingFactor;
            EdgesAboveBlocks = city.EdgeLayoutSettings.EdgesAboveBlocks;
            Tension = city.EdgeLayoutSettings.Tension;
            RDP = city.EdgeLayoutSettings.RDP;

            TubularSegments = city.EdgeSelectionSettings.TubularSegments;
            Radius = city.EdgeSelectionSettings.Radius;
            RadialSegments = city.EdgeSelectionSettings.RadialSegments;
            isEdgeSelectable = city.EdgeSelectionSettings.AreSelectable;


            if (city.GetType() == typeof(SEECity))
            {
                gxlPath = ((SEECity)city).GXLPath;
                csvPath = ((SEECity)city).CSVPath;
            }

            if (city.GetType() == typeof(SEECityEvolution))
            {
                maxRevisionsToLoad = ((SEECityEvolution)city).MaxRevisionsToLoad;
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

            city.ErosionSettings.ArchitectureIssue = ArchitectureIssue;
            city.ErosionSettings.CloneIssue = CloneIssue;
            city.ErosionSettings.CycleIssue = CycleIssue;
            city.ErosionSettings.Dead_CodeIssue = Dead_CodeIssue;
            city.ErosionSettings.MetricIssue = MetricIssue;
            city.ErosionSettings.StyleIssue = StyleIssue;
            city.ErosionSettings.UniversalIssue = UniversalIssue;

            city.ErosionSettings.ArchitectureIssue_SUM = ArchitectureIssue_SUM;
            city.ErosionSettings.CloneIssue_SUM = CloneIssue_SUM;
            city.ErosionSettings.CycleIssue_SUM = CycleIssue_SUM;
            city.ErosionSettings.Dead_CodeIssue_SUM = Dead_CodeIssue_SUM;
            city.ErosionSettings.MetricIssue_SUM = MetricIssue_SUM;
            city.ErosionSettings.StyleIssue_SUM = StyleIssue_SUM;
            city.ErosionSettings.UniversalIssue_SUM = UniversalIssue_SUM;

            //city.innerNodeAttributes.styleMetric = InnerNodeStyleMetric;

            city.LeafNodeSettings.MinimalBlockLength = MinimalBlockLength;
            city.LeafNodeSettings.MaximalBlockLength = MaximalBlockLength;

            //city.nodeLayoutSettings.leafKind = LeafObjects;
            //city.nodeLayoutSettings.innerKind = InnerNodeObjects;

            city.NodeLayoutSettings.Kind = NodeLayout;
            city.EdgeLayoutSettings.Kind = EdgeLayout;
            city.ZScoreScale = ZScoreScale;
            city.ScaleOnlyLeafMetrics = ScaleOnlyLeafMetrics;

            city.EdgeLayoutSettings.EdgeWidth = EdgeWidth;
            city.ErosionSettings.ShowInnerErosions = ShowInnerErosions;
            city.ErosionSettings.ShowLeafErosions = ShowLeafErosions;
            city.ErosionSettings.LoadDashboardMetrics = LoadDashboardMetrics;
            city.ErosionSettings.IssuesAddedFromVersion = IssuesAddedFromVersion;
            city.ErosionSettings.OverrideMetrics = OverrideMetrics;
            city.ErosionSettings.ErosionScalingFactor = ErosionScalingFactor;
            city.EdgeLayoutSettings.EdgesAboveBlocks = EdgesAboveBlocks;
            city.EdgeLayoutSettings.Tension = Tension;
            city.EdgeLayoutSettings.RDP = RDP;

            city.EdgeSelectionSettings.TubularSegments = TubularSegments;
            city.EdgeSelectionSettings.Radius = Radius;
            city.EdgeSelectionSettings.RadialSegments = RadialSegments;
            city.EdgeSelectionSettings.AreSelectable = isEdgeSelectable;

            if (t == typeof(SEECity))
            {
                ((SEECity)city).LoadAndDrawGraph();
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