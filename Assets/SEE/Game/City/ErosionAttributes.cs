using System;
using System.Collections.Generic;
using SEE.DataModel.DG;
using SEE.Utils.Config;
using UnityEngine;
using UnityEngine.Serialization;

namespace SEE.Game.City
{
    /// <summary>
    /// Axivion's software erosion issues shown as icons above nodes.
    /// </summary>
    [Serializable]
    public class ErosionAttributes : VisualAttributes
    {
        /// <summary>
        /// Whether inner erosions should be visible above node blocks.
        /// </summary>
        public bool ShowInnerErosions;

        /// <summary>
        /// Whether leaf erosions should be visible above node blocks.
        /// </summary>
        public bool ShowLeafErosions;

        /// <summary>
        /// The maximal value for <see cref="ErosionScalingFactor"/>.
        /// </summary>
        public const float MaxErosionScalingFactor = 5.0f;

        /// <summary>
        /// Factor by which erosion icons shall be scaled.
        /// </summary>
        [Range(0.0f, MaxErosionScalingFactor)]
        public float ErosionScalingFactor = 1.5f;

        /// <summary>
        /// Whether code issues from the Axivion Dashboard should be downloaded and shown in code viewers.
        /// </summary>
        [FormerlySerializedAs("ShowIssuesInCodeWindow")]
        public bool ShowDashboardIssuesInCodeWindow = false;

        /// <summary>
        /// The attribute name of the metric representing architecture violations.
        /// </summary>
        public string ArchitectureIssue = NumericAttributeNames.ArchitectureViolations.Name(); // serialized by Unity
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
        public string DeadCodeIssue = NumericAttributeNames.DeadCode.Name(); // serialized by Unity
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

        /// <summary>
        /// The attribute name of the metric representing LSP hints.
        /// </summary>
        public string LspHint = NumericAttributeNames.LspHint.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing LSP infos.
        /// </summary>
        public string LspInfo = NumericAttributeNames.LspInfo.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing LSP warnings.
        /// </summary>
        public string LspWarning = NumericAttributeNames.LspWarning.Name(); // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing LSP errors.
        /// </summary>
        public string LspError = NumericAttributeNames.LspError.Name(); // serialized by Unity

        //-----------------------------------------------------------------------
        // Software erosion issues shown as icons on Donut charts for inner nodes
        //-----------------------------------------------------------------------
        public const string SumPostfix = "_SUM";
        /// <summary>
        /// The attribute name of the metric representing the sum of all architecture violations
        /// for an inner node.
        /// </summary>
        public string ArchitectureIssueSum = NumericAttributeNames.ArchitectureViolations.Name() + SumPostfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all clones
        /// for an inner node.
        /// </summary>
        public string CloneIssueSum = NumericAttributeNames.Clone.Name() + SumPostfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all cycles
        /// for an inner node.
        /// </summary>
        public string CycleIssueSum = NumericAttributeNames.Cycle.Name() + SumPostfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all dead entities
        /// for an inner node.
        /// </summary>
        public string DeadCodeIssueSum = NumericAttributeNames.DeadCode.Name() + SumPostfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all metric violations
        /// for an inner node.
        /// </summary>
        public string MetricIssueSum = NumericAttributeNames.Metric.Name() + SumPostfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all style violations
        /// for an inner node.
        /// </summary>
        public string StyleIssueSum = NumericAttributeNames.Style.Name() + SumPostfix; // serialized by Unity
        /// <summary>
        /// The attribute name of the metric representing the sum of all other kinds of
        /// software erosions for an inner node.
        /// </summary>
        public string UniversalIssueSum = NumericAttributeNames.Universal.Name() + SumPostfix; // serialized by Unity

        public override void Save(ConfigWriter writer, string label)
        {
            writer.BeginGroup(label);
            writer.Save(ShowInnerErosions, showInnerErosionsLabel);
            writer.Save(ShowLeafErosions, showLeafErosionsLabel);
            writer.Save(ShowDashboardIssuesInCodeWindow, showIssuesInCodeWindowLabel);
            writer.Save(ErosionScalingFactor, erosionScalingFactorLabel);

            writer.Save(StyleIssue, styleIssueLabel);
            writer.Save(UniversalIssue, universalIssueLabel);
            writer.Save(MetricIssue, metricIssueLabel);
            writer.Save(DeadCodeIssue, deadCodeIssueLabel);
            writer.Save(CycleIssue, cycleIssueLabel);
            writer.Save(CloneIssue, cloneIssueLabel);
            writer.Save(ArchitectureIssue, architectureIssueLabel);
            writer.Save(LspHint, lspHintLabel);
            writer.Save(LspInfo, lspInfoLabel);
            writer.Save(LspWarning, lspWarningLabel);
            writer.Save(LspError, lspErrorLabel);

            writer.Save(StyleIssueSum, styleIssueSumLabel);
            writer.Save(UniversalIssueSum, universalIssueSumLabel);
            writer.Save(MetricIssueSum, metricIssueSumLabel);
            writer.Save(DeadCodeIssueSum, deadCodeIssueSumLabel);
            writer.Save(CycleIssueSum, cycleIssueSumLabel);
            writer.Save(CloneIssueSum, cloneIssueSumLabel);
            writer.Save(ArchitectureIssueSum, architectureIssueSumLabel);
            writer.EndGroup();
        }

        public override void Restore(Dictionary<string, object> attributes, string label)
        {
            if (attributes.TryGetValue(label, out object dictionary))
            {
                Dictionary<string, object> values = dictionary as Dictionary<string, object>;

                ConfigIO.Restore(values, showInnerErosionsLabel, ref ShowInnerErosions);
                ConfigIO.Restore(values, showLeafErosionsLabel, ref ShowLeafErosions);
                ConfigIO.Restore(values, showIssuesInCodeWindowLabel, ref ShowDashboardIssuesInCodeWindow);
                ConfigIO.Restore(values, erosionScalingFactorLabel, ref ErosionScalingFactor);

                ConfigIO.Restore(values, styleIssueLabel, ref StyleIssue);
                ConfigIO.Restore(values, universalIssueLabel, ref UniversalIssue);
                ConfigIO.Restore(values, metricIssueLabel, ref MetricIssue);
                ConfigIO.Restore(values, deadCodeIssueLabel, ref DeadCodeIssue);
                ConfigIO.Restore(values, cycleIssueLabel, ref CycleIssue);
                ConfigIO.Restore(values, cloneIssueLabel, ref CloneIssue);
                ConfigIO.Restore(values, architectureIssueLabel, ref ArchitectureIssue);
                ConfigIO.Restore(values, lspHintLabel, ref LspHint);
                ConfigIO.Restore(values, lspInfoLabel, ref LspInfo);
                ConfigIO.Restore(values, lspWarningLabel, ref LspWarning);
                ConfigIO.Restore(values, lspErrorLabel, ref LspError);

                ConfigIO.Restore(values, styleIssueSumLabel, ref StyleIssueSum);
                ConfigIO.Restore(values, universalIssueSumLabel, ref UniversalIssueSum);
                ConfigIO.Restore(values, metricIssueSumLabel, ref MetricIssueSum);
                ConfigIO.Restore(values, deadCodeIssueSumLabel, ref DeadCodeIssueSum);
                ConfigIO.Restore(values, cycleIssueSumLabel, ref CycleIssueSum);
                ConfigIO.Restore(values, cloneIssueSumLabel, ref CloneIssueSum);
                ConfigIO.Restore(values, architectureIssueSumLabel, ref ArchitectureIssueSum);
            }
        }

        private const string showLeafErosionsLabel = "ShowLeafErosions";
        private const string showInnerErosionsLabel = "ShowInnerErosions";
        private const string erosionScalingFactorLabel = "ErosionScalingFactor";
        private const string showIssuesInCodeWindowLabel = "ShowDashboardIssuesInCodeWindow";

        private const string styleIssueLabel = "StyleIssue";
        private const string universalIssueLabel = "UniversalIssue";
        private const string metricIssueLabel = "MetricIssue";
        private const string deadCodeIssueLabel = "Dead_CodeIssue";
        private const string cycleIssueLabel = "CycleIssue";
        private const string cloneIssueLabel = "CloneIssue";
        private const string architectureIssueLabel = "ArchitectureIssue";
        private const string lspHintLabel = "LspHint";
        private const string lspInfoLabel = "LspInfo";
        private const string lspWarningLabel = "LspWarning";
        private const string lspErrorLabel = "LspError";

        private const string styleIssueSumLabel = "StyleIssue_SUM";
        private const string universalIssueSumLabel = "UniversalIssue_SUM";
        private const string metricIssueSumLabel = "MetricIssue_SUM";
        private const string deadCodeIssueSumLabel = "Dead_CodeIssue_SUM";
        private const string cycleIssueSumLabel = "CycleIssue_SUM";
        private const string cloneIssueSumLabel = "CloneIssue_SUM";
        private const string architectureIssueSumLabel = "ArchitectureIssue_SUM";
    }
}
