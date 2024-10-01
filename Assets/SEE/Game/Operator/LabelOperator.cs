using DG.Tweening;
using SEE.GO;
using SEE.Utils;
using TMPro;
using UnityEngine;

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
        private const string labelPrefix = "Label ";

        /// <summary>
        /// Updates the position of the attached label to the given <paramref name="labelBase"/>,
        /// including its text and line.
        /// </summary>
        /// <param name="labelBase">The position on the surface of the node where the label should be placed.</param>
        public void UpdateLabelLayout(Vector3 labelBase)
        {
            DesiredLabelStartLinePosition = labelBase;
            // Adding a small duration to make the label appear more smoothly.
            float duration = ToDuration(0.2f);
            labelTextPosition.AnimateTo(DesiredLabelTextPosition, duration);
            labelStartLinePosition.AnimateTo(DesiredLabelStartLinePosition, duration);
            labelEndLinePosition.AnimateTo(DesiredLabelEndLinePosition, duration);
        }

        /// <summary>
        /// Returns true if the label is not empty.
        /// </summary>
        /// <returns>True if the label is not empty.</returns>
        public bool LabelIsNotEmpty()
        {
            return !string.IsNullOrWhiteSpace(labelText?.text);
        }

        /// <summary>
        /// Creates and prepares the <see cref="nodeLabel"/> game object along with its components.
        /// If <see cref="nodeLabel"/> already exists, nothing will happen.
        /// </summary>
        private void PrepareLabel()
        {
            Color textColor = UnityEngine.Color.white;

            string shownText = Node?.SourceName ?? gameObject.name;

            nodeLabel = transform.Find(labelPrefix + shownText)?.gameObject;
            if (nodeLabel == null)
            {
                // The text of the label appears above the hull of the labeled game object
                // which is including the heights of its children. A line will be drawn from the
                // roof of the game object excluding its children to the label text.
                // First we create the label.
                // We define starting and ending positions for the animation.
                Vector3 startLabelPosition = gameObject.GetTop();
                float fontSize = Node != null ? City.NodeTypes[Node.Type].LabelSettings.FontSize : LabelAttributes.DefaultFontSize;
                nodeLabel = TextFactory.GetTextWithSize(City,
                                                        shownText,
                                                        startLabelPosition,
                                                        fontSize,
                                                        lift: true,
                                                        overlay: true,
                                                        textColor: textColor);
                nodeLabel.name = labelPrefix + shownText;
                nodeLabel.transform.SetParent(gameObject.transform);

                // Second, add connecting line between "roof" of the game object and the text.
                Vector3 startLinePosition = gameObject.GetRoofCenter();
                GameObject line = new()
                {
                    name = $"{labelPrefix}{shownText} (Connecting Line)"
                };
                LineFactory.Draw(line, new[] { startLinePosition, startLinePosition }, 0.01f, City.LabelLineMaterial);
                line.transform.SetParent(nodeLabel.transform);

                // The nodeLabel and its child edge must inherit the portal of gameObject.
                Portal.GetPortal(gameObject, out Vector2 leftFront, out Vector2 rightBack);
                Portal.SetPortal(nodeLabel, leftFront, rightBack);

                // Make it invisible, initially.
                if (nodeLabel.TryGetComponentOrLog(out labelText) && line.TryGetComponentOrLog(out labelLineRenderer))
                {
                    labelText.alpha = 0f;
                    labelLineRenderer.startColor = labelLineRenderer.startColor.WithAlpha(0f);
                    labelLineRenderer.endColor = labelLineRenderer.endColor.WithAlpha(0f);
                }
            }
            else if (!nodeLabel.activeSelf)
            {
                nodeLabel.SetActive(true);
            }
        }

        /// <summary>
        /// Target position of the label's text.
        /// </summary>
        private Vector3 DesiredLabelTextPosition
        {
            get
            {
                Vector3 endLabelPosition = DesiredLabelStartLinePosition;
                if (labelAlpha.TargetValue > 0)
                {
                    // Only put line and label up if the label should actually be shown.
                    endLabelPosition.y += Node != null ? City.NodeTypes[Node.Type].LabelSettings.Distance : LabelAttributes.DefaultDistance;
                }

                return endLabelPosition;
            }
        }

        /// <summary>
        /// Desired starting position of the label's line.
        /// </summary>
        private Vector3 DesiredLabelStartLinePosition;

        /// <summary>
        /// Desired end position of the label's line.
        /// </summary>
        private Vector3 DesiredLabelEndLinePosition
        {
            get
            {
                // Due to the line not using world space, we need to transform its position accordingly.
                Vector3 endLinePosition = labelTextPosition.TargetValue;
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
            DOTween.To(() => labelLineRenderer.GetPosition(0), p => labelLineRenderer.SetPosition(0, p), startPosition,
                duration).Play()
        };

        private Tween[] AnimateLabelEndLinePositionAction(Vector3 endPosition, float duration) => new Tween[]
        {
            DOTween.To(() => labelLineRenderer.GetPosition(1), p => labelLineRenderer.SetPosition(1, p), endPosition,
                duration).Play()
        };

        private Tween[] AnimateLabelTextPositionAction(Vector3 position, float duration) => new Tween[]
        {
            nodeLabel.transform.DOMove(position, duration).Play()
        };
    }
}
