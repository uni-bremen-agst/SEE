using System.Linq;
using DG.Tweening;
using SEE.Controls.Actions;
using SEE.GO;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Valve.VR.InteractionSystem;

namespace SEE.Game.Operator
{
    /// <summary>
    /// Part of the <see cref="NodeOperator"/> class which is responsible for managing node labels.
    /// </summary>
    public partial class NodeOperator
    {
        /// <summary>
        /// The game object containing the node's label.
        /// </summary>
        private GameObject nodeLabel;

        /// <summary>
        /// The line renderer responsible for the line which connects the node label's text to the node.
        /// </summary>
        private LineRenderer labelLineRenderer;

        /// <summary>
        /// The text of the node's label.
        /// </summary>
        private TextMeshPro labelText;

        /// <summary>
        /// Prefix for game object names of node labels.
        /// </summary>
        private const string LABEL_PREFIX = "Label ";

        /// <summary>
        /// Updates the position of the attached label, including its text and line.
        /// </summary>
        /// <param name="duration"></param>
        private void UpdateLabelLayout(float duration)
        {
            // Assumption: There are only very few active labels, compared to all active and inactive labels
            //             that may exist in the descendants of this node. Hence, we go through all active ones.
            foreach (NodeOperator nodeOperator in ShowLabel.DisplayedLabelOperators.Where(x => x.LabelAlpha.TargetValue > 0f))
            {
                nodeOperator.LabelTextPosition.AnimateTo(nodeOperator.DesiredLabelTextPosition, duration);
                nodeOperator.LabelStartLinePosition.AnimateTo(nodeOperator.DesiredLabelStartLinePosition, duration);
                nodeOperator.LabelEndLinePosition.AnimateTo(nodeOperator.DesiredLabelEndLinePosition, duration);
            }
        }

        /// <summary>
        /// Creates and prepares the <see cref="nodeLabel"/> game object along with its components.
        /// If <see cref="nodeLabel"/> already exists, nothing will happen.
        /// </summary>
        private void PrepareLabel()
        {
            Color textColor = Color.white; // .ColorWithAlpha(1f)
            Color lineColor = Color.white;

            string shownText = Node.SourceName;

            nodeLabel = transform.Find(LABEL_PREFIX + shownText)?.gameObject;
            if (nodeLabel == null)
            {
                // The text of the label appears above the hull of the labeled game object
                // which is including the heights of its children. A line will be drawn from the
                // roof of the game object excluding its children to the label text.
                // First we create the label.
                // We define starting and ending positions for the animation.
                Vector3 startLabelPosition = gameObject.GetTop();
                nodeLabel = TextFactory.GetTextWithSize(shownText,
                                                        startLabelPosition,
                                                        City.NodeTypes[Node.Type].LabelSettings.FontSize,
                                                        lift: true,
                                                        textColor: textColor);
                nodeLabel.name = LABEL_PREFIX + shownText;
                nodeLabel.transform.SetParent(gameObject.transform);

                // Second, add connecting line between "roof" of the game object and the text.
                Vector3 startLinePosition = gameObject.GetRoofCenter();
                GameObject line = new()
                {
                    name = $"{LABEL_PREFIX}{shownText} (Connecting Line)"
                };
                LineFactory.Draw(line, new[] { startLinePosition, startLinePosition }, 0.01f,
                                 LineMaterial(lineColor));
                line.transform.SetParent(nodeLabel.transform);

                // The nodeLabel and its child edge must inherit the portal of gameObject.
                Portal.GetPortal(gameObject, out Vector2 leftFront, out Vector2 rightBack);
                Portal.SetPortal(nodeLabel, leftFront, rightBack);

                // Make it invisible, initially.
                if (nodeLabel.TryGetComponentOrLog(out labelText) && line.TryGetComponentOrLog(out labelLineRenderer))
                {
                    labelText.alpha = 0f;
                    labelLineRenderer.startColor = lineColor.ColorWithAlpha(0f);
                    labelLineRenderer.endColor = lineColor.ColorWithAlpha(0f);
                }
            }
        }

        /// <summary>
        /// Material for the line connecting a node and its label. We use the
        /// exactly the same material for all.
        /// </summary>
        private static Material lineMaterial;

        /// <summary>
        /// Returns the material for the line connecting a node and its label.
        /// </summary>
        /// <param name="lineColor"></param>
        /// <returns></returns>
        private static Material LineMaterial(Color lineColor)
        {
            if (lineMaterial == null)
            {
                lineMaterial = Materials.New(Materials.ShaderType.TransparentLine, lineColor, texture: null,
                                             renderQueueOffset: (int)(RenderQueue.Transparent + 1));
            }
            return lineMaterial;
        }

        /// <summary>
        /// Target position of the label's text.
        /// </summary>
        private Vector3 DesiredLabelTextPosition
        {
            get
            {
                Vector3 endLabelPosition = gameObject.GetTop(t => !t.name.StartsWith(LABEL_PREFIX));
                if (LabelAlpha.TargetValue > 0)
                {
                    // Only put line and label up if the label should actually be shown.
                    endLabelPosition.y += City.NodeTypes[Node.Type].LabelSettings.Distance;
                }

                return endLabelPosition;
            }
        }

        /// <summary>
        /// Desired starting position of the label's line.
        /// </summary>
        private Vector3 DesiredLabelStartLinePosition => gameObject.GetRoofCenter();

        /// <summary>
        /// Desired end position of the label's line.
        /// </summary>
        private Vector3 DesiredLabelEndLinePosition
        {
            get
            {
                // Due to the line not using world space, we need to transform its position accordingly.
                Vector3 endLinePosition = LabelTextPosition.TargetValue;
                float labelTextExtent = labelText.textBounds.extents.y;
                endLinePosition.y -= labelTextExtent * 1.3f; // add slight gap to make it more aesthetic
                return endLinePosition;
            }
        }

        private Tween[] AnimateLabelAlphaAction(float alpha, float duration) => new Tween[]
        {
            // Animated label to move to top and fade in
            DOTween.ToAlpha(() => labelText.color, color => labelText.color = color, alpha, duration).Play(),
            // Lower start of line should be visible almost immediately due to reduced alpha (smooth transition)
            DOTween.ToAlpha(() => labelLineRenderer.startColor, c => labelLineRenderer.startColor = c, alpha, duration * 0.1f).Play(),
            DOTween.ToAlpha(() => labelLineRenderer.endColor, c => labelLineRenderer.endColor = c, alpha, duration).Play(),
        };

        private Tween[] AnimateLabelStartLinePositionAction(Vector3 startPosition, float duration) => new Tween[]
        {
            DOTween.To(() => labelLineRenderer.GetPosition(0), p => labelLineRenderer.SetPosition(0, p), startPosition, duration).Play()
        };

        private Tween[] AnimateLabelEndLinePositionAction(Vector3 endPosition, float duration) => new Tween[]
        {
            DOTween.To(() => labelLineRenderer.GetPosition(1), p => labelLineRenderer.SetPosition(1, p), endPosition, duration).Play()
        };

        private Tween[] AnimateLabelTextPositionAction(Vector3 position, float duration) => new Tween[]
        {
            nodeLabel.transform.DOMove(position, duration).Play()
        };
    }
}