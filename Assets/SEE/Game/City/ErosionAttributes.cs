using System;
using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Utils;
using UnityEngine;

namespace SEE.Game.City
{
    /// <summary>
    /// Axivion's software erosion issues shown as icons above nodes.
    /// </summary>
    [Serializable]
    public class ErosionAttributes : VisualAttributes
    {
        /// <summary>
        /// Whether erosions should be visible above inner node blocks.
        /// </summary>
        public bool ShowInnerErosions = false;

        /// <summary>
        /// Whether erosions should be visible above leaf node blocks.
        /// </summary>
        public bool ShowLeafErosions = false;

        /// <summary>
        /// Whether metrics shall be retrieved from the dashboard.
        /// This includes erosion data.
        /// </summary>
        public bool LoadDashboardMetrics = false;

        /// <summary>
        /// If empty, all issues will be retrieved. Otherwise, only those issues which have been added from
        /// the given version to the most recent one will be loaded.
        /// </summary>
        public string IssuesAddedFromVersion = "";

        /// <summary>
        /// Whether metrics retrieved from the dashboard shall override existing metrics.
        /// </summary>
        public bool OverrideMetrics = true;

        /// <summary>
        /// Factor by which erosion icons shall be scaled.
        /// </summary>
        [Range(0.0f, float.MaxValue)]
        public float ErosionScalingFactor = 1.5f;

        /// <summary>
        /// Whether code issues should be downloaded and shown in code viewers.
        /// </summary>
        public bool ShowIssuesInCodeWindow = false;

        /// <summary>
        /// The attribute name of the metric representing architecture violations.
        /// </summary>
        public string ArchitectureIssue = NumericAttributeNames.Architecture_Violations.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing duplicated code.
        /// </summary>
        public string CloneIssue = NumericAttributeNames.Clone.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing cylces.
        /// </summary>
        public string CycleIssue = NumericAttributeNames.Cycle.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing dead code.
        /// </summary>
        public string Dead_CodeIssue = NumericAttributeNames.Dead_Code.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing metric violations.
        /// </summary>
        public string MetricIssue = NumericAttributeNames.Metric.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing code-style violations.
        /// </summary>
        public string StyleIssue = NumericAttributeNames.Style.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing other kinds of violations.
        /// </summary>
        public string UniversalIssue = NumericAttributeNames.Universal.Name(); // serialized by Unity

        //-----------------------------------------------------------------------
        // Software erosion issues shown as icons on Donut charts for inner nodes
        //-----------------------------------------------------------------------
        public const string SUM_Postfix = "_SUM";
        /// <summary>
        /// The attribute name of the metric representing the sum of all architecture violations
        /// for an inner node.
        /// </summary>
        public string ArchitectureIssue_SUM = NumericAttributeNames.Architecture_Violations.Name() + SUM_Postfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all clones
        /// for an inner node.
        /// </summary>
        public string CloneIssue_SUM = NumericAttributeNames.Clone.Name() + SUM_Postfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all cycles
        /// for an inner node.
        /// </summary>
        public string CycleIssue_SUM = NumericAttributeNames.Cycle.Name() + SUM_Postfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all dead entities
        /// for an inner node.
        /// </summary>
        public string Dead_CodeIssue_SUM = NumericAttributeNames.Dead_Code.Name() + SUM_Postfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all metric violations
        /// for an inner node.
        /// </summary>
        public string MetricIssue_SUM = NumericAttributeNames.Metric.Name() + SUM_Postfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all style violations
        /// for an inner node.
        /// </summary>
        public string StyleIssue_SUM = NumericAttributeNames.Style.Name() + SUM_Postfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all other kinds of
        /// software erosions for an inner node.
        /// </summary>
        public string UniversalIssue_SUM = NumericAttributeNames.Universal.Name() + SUM_Postfix; // serialized by Unity

        public override void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(ShowInnerErosions, ShowInnerErosionsLabel);
            writer.Save(ShowLeafErosions, ShowLeafErosionsLabel);
            writer.Save(LoadDashboardMetrics, LoadDashboardMetricsLabel);
            writer.Save(OverrideMetrics, OverrideMetricsLabel);
            writer.Save(ShowIssuesInCodeWindow, ShowIssuesInCodeWindowLabel);
            writer.Save(IssuesAddedFromVersion, IssuesFromVersionLabel);
            writer.Save(ErosionScalingFactor, ErosionScalingFactorLabel);

            writer.Save(StyleIssue, StyleIssueLabel);
            writer.Save(UniversalIssue, UniversalIssueLabel);
            writer.Save(MetricIssue, MetricIssueLabel);
            writer.Save(Dead_CodeIssue, Dead_CodeIssueLabel);
            writer.Save(CycleIssue, CycleIssueLabel);
            writer.Save(CloneIssue, CloneIssueLabel);
            writer.Save(ArchitectureIssue, ArchitectureIssueLabel);

            writer.Save(StyleIssue_SUM, StyleIssue_SUMLabel);
            writer.Save(UniversalIssue_SUM, UniversalIssue_SUMLabel);
            writer.Save(MetricIssue_SUM, MetricIssue_SUMLabel);
            writer.Save(Dead_CodeIssue_SUM, Dead_CodeIssue_SUMLabel);
            writer.Save(CycleIssue_SUM, CycleIssue_SUMLabel);
            writer.Save(CloneIssue_SUM, CloneIssue_SUMLabel);
            writer.Save(ArchitectureIssue_SUM, ArchitectureIssue_SUMLabel);
            writer.EndGroup();
        }

        public override void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                ConfigIO.Restore(values, ShowInnerErosionsLabel, ref ShowInnerErosions);
                ConfigIO.Restore(values, ShowLeafErosionsLabel, ref ShowLeafErosions);
                ConfigIO.Restore(values, LoadDashboardMetricsLabel, ref LoadDashboardMetrics);
                ConfigIO.Restore(values, OverrideMetricsLabel, ref OverrideMetrics);
                ConfigIO.Restore(values, ShowIssuesInCodeWindowLabel, ref ShowIssuesInCodeWindow);
                ConfigIO.Restore(values, IssuesFromVersionLabel, ref IssuesAddedFromVersion);
                ConfigIO.Restore(values, ErosionScalingFactorLabel, ref ErosionScalingFactor);

                ConfigIO.Restore(values, StyleIssueLabel, ref StyleIssue);
                ConfigIO.Restore(values, UniversalIssueLabel, ref UniversalIssue);
                ConfigIO.Restore(values, MetricIssueLabel, ref MetricIssue);
                ConfigIO.Restore(values, Dead_CodeIssueLabel, ref Dead_CodeIssue);
                ConfigIO.Restore(values, CycleIssueLabel, ref CycleIssue);
                ConfigIO.Restore(values, CloneIssueLabel, ref CloneIssue);
                ConfigIO.Restore(values, ArchitectureIssueLabel, ref ArchitectureIssue);

                ConfigIO.Restore(values, StyleIssue_SUMLabel, ref StyleIssue_SUM);
                ConfigIO.Restore(values, UniversalIssue_SUMLabel, ref UniversalIssue_SUM);
                ConfigIO.Restore(values, MetricIssue_SUMLabel, ref MetricIssue_SUM);
                ConfigIO.Restore(values, Dead_CodeIssue_SUMLabel, ref Dead_CodeIssue_SUM);
                ConfigIO.Restore(values, CycleIssue_SUMLabel, ref CycleIssue_SUM);
                ConfigIO.Restore(values, CloneIssue_SUMLabel, ref CloneIssue_SUM);
                ConfigIO.Restore(values, ArchitectureIssue_SUMLabel, ref ArchitectureIssue_SUM);
            }
        }

        private const string ShowLeafErosionsLabel = "ShowLeafErosions";
        private const string ShowInnerErosionsLabel = "ShowInnerErosions";
        private const string ErosionScalingFactorLabel = "ErosionScalingFactor";
        private const string LoadDashboardMetricsLabel = "LoadDashboardMetrics";
        private const string IssuesFromVersionLabel = "IssuesAddedFromVersion";
        private const string OverrideMetricsLabel = "OverrideMetrics";
        private const string ShowIssuesInCodeWindowLabel = "ShowIssuesInCodeWindow";

        private const string StyleIssueLabel = "StyleIssue";
        private const string UniversalIssueLabel = "UniversalIssue";
        private const string MetricIssueLabel = "MetricIssue";
        private const string Dead_CodeIssueLabel = "Dead_CodeIssue";
        private const string CycleIssueLabel = "CycleIssue";
        private const string CloneIssueLabel = "CloneIssue";
        private const string ArchitectureIssueLabel = "ArchitectureIssue";

        private const string StyleIssue_SUMLabel = "StyleIssue_SUM";
        private const string UniversalIssue_SUMLabel = "UniversalIssue_SUM";
        private const string MetricIssue_SUMLabel = "MetricIssue_SUM";
        private const string Dead_CodeIssue_SUMLabel = "Dead_CodeIssue_SUM";
        private const string CycleIssue_SUMLabel = "CycleIssue_SUM";
        private const string CloneIssue_SUMLabel = "CloneIssue_SUM";
        private const string ArchitectureIssue_SUMLabel = "ArchitectureIssue_SUM";
    }
}
