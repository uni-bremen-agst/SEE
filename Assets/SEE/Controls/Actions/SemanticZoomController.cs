using System;
using System.Collections.Generic;
using SEE.GO;
using SEE.Utils;
using UnityEngine;

namespace SEE.Controls.Actions
{
    /// <summary>
    /// Detail level determines what visual information is shown for a node.
    /// Calculated from both camera distance and zoom factor.
    /// </summary>
    public enum DetailLevel
    {
        Overview,  // Far away or zoomed out - minimal detail
        Basic,     // Medium distance or some zoom - show basic info
        Medium,    // Close or good zoom - show more details
        Full       // Very close and high zoom - show everything
    }

    /// <summary>
    /// Manages semantic zoom by tracking camera distance to nodes and determining
    /// detail levels based on both distance and zoom factor.
    /// Attach this component to the city root GameObject (e.g., "N SEE").
    /// </summary>
    public class SemanticZoomController : MonoBehaviour
    {
        [Header("Distance Thresholds (World Units)")]
        [SerializeField] private float veryCloseDistance = 5f;
        [SerializeField] private float closeDistance = 15f;
        [SerializeField] private float mediumDistance = 30f;
        [SerializeField] private float farDistance = 50f;

        [Header("Zoom Factor Thresholds")]
        [SerializeField] private float lowZoomThreshold = 1.0f;
        [SerializeField] private float mediumZoomThreshold = 2.0f;
        [SerializeField] private float highZoomThreshold = 4.0f;

        [Header("Performance")]
        [SerializeField] private float updateInterval = 0.1f;
        [SerializeField] private int maxNodesPerFrame = 0; // 0 = update all nodes per cycle

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        [SerializeField] private bool showDistanceGizmos = false;
        [SerializeField] private bool showClosestNodeLines = false;

        private Camera mainCamera;
        private ZoomAction zoomAction; // References player's ZoomAction to get current zoom factor
        private readonly List<CachedNodeInfo> cachedNodes = new List<CachedNodeInfo>();
        private float lastUpdateTime;
        private float currentZoomFactor = 1.0f; // Cached from ZoomAction each update
        private int updateBatchIndex = 0; // For batched node updates
        private bool isInitialized = false;

        // Global event for all detail level changes in this city
        public event Action<CachedNodeInfo, DetailLevel, DetailLevel> OnDetailLevelChanged;

        private void Start()
        {
            mainCamera = MainCamera.Camera;
            if (mainCamera == null)
            {
                Debug.LogError($"[SemanticZoomController] No main camera found for city '{gameObject.name}'");
                enabled = false;
                return;
            }

            zoomAction = FindObjectOfType<ZoomAction>();
            if (zoomAction == null)
            {
                Debug.LogError($"[SemanticZoomController] No ZoomAction found for city '{gameObject.name}'");
                enabled = false;
                return;
            }

            DiscoverNodes();

            if (cachedNodes.Count == 0)
            {
                Debug.LogWarning($"[SemanticZoomController] No nodes discovered in city '{gameObject.name}'");
            }

            isInitialized = true;
            lastUpdateTime = Time.realtimeSinceStartup;

            if (debugMode)
            {
                Debug.Log($"[SemanticZoomController] Initialized for '{gameObject.name}' with {cachedNodes.Count} nodes");
            }
        }

        private void FixedUpdate()
        {
            // Use FixedUpdate for frame-rate independence (follows ZoomAction pattern)
            if (!isInitialized || Time.realtimeSinceStartup - lastUpdateTime < updateInterval)
            {
                return;
            }

            lastUpdateTime = Time.realtimeSinceStartup;
            UpdateZoomFactor();
            UpdateNodeDistances();
        }

        /// <summary>
        /// Discovers all nodes in this city's hierarchy and caches them.
        /// Called once during Start() to avoid repeated hierarchy traversal.
        /// </summary>
        private void DiscoverNodes()
        {
            cachedNodes.Clear();
            NodeRef[] nodeRefs = GetComponentsInChildren<NodeRef>(includeInactive: true);

            foreach (NodeRef nodeRef in nodeRefs)
            {
                // Skip invalid nodes and root nodes (root represents entire graph, not a class)
                if (nodeRef == null || nodeRef.Value == null || nodeRef.Value.IsRoot())
                {
                    continue;
                }

                CachedNodeInfo nodeInfo = new CachedNodeInfo(nodeRef.gameObject, nodeRef);
                // Subscribe to individual node events and forward to controller event
                nodeInfo.OnDetailLevelChanged += (node, oldLevel, newLevel) =>
                {
                    OnDetailLevelChanged?.Invoke(node, oldLevel, newLevel);

                    if (debugMode)
                    {
                        Debug.Log($"[SemanticZoom] {node.NodeGameObject.name}: {oldLevel} → {newLevel}");
                    }
                };
                cachedNodes.Add(nodeInfo);
            }
        }

        /// <summary>
        /// Queries ZoomAction for current zoom factor and caches it.
        /// Zoom factor combines with distance to determine detail levels.
        /// </summary>
        private void UpdateZoomFactor()
        {
            if (zoomAction == null)
            {
                return;
            }

            float newZoomFactor = zoomAction.GetCurrentZoomFactor(transform);

            if (debugMode)
            {
                if (!Mathf.Approximately(newZoomFactor, currentZoomFactor))
                {
                    Debug.Log($"[SemanticZoomController] Zoom: {currentZoomFactor:F2}x → {newZoomFactor:F2}x");
                }
                else if (Time.frameCount % 300 == 0)
                {
                    Debug.Log($"[SemanticZoomController] Querying zoom for '{transform.name}' = {newZoomFactor:F2}x");
                }
            }

            currentZoomFactor = newZoomFactor;
        }

        /// <summary>
        /// Calculates and updates distance from camera to each node.
        /// Supports batching via maxNodesPerFrame for performance with large cities.
        /// </summary>
        private void UpdateNodeDistances()
        {
            if (mainCamera == null || cachedNodes.Count == 0)
            {
                return;
            }

            int nodesToUpdate = maxNodesPerFrame > 0 ? maxNodesPerFrame : cachedNodes.Count;

            for (int i = 0; i < nodesToUpdate && i < cachedNodes.Count; i++)
            {
                int index = (updateBatchIndex + i) % cachedNodes.Count;
                CachedNodeInfo nodeInfo = cachedNodes[index];

                if (nodeInfo.NodeTransform == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(mainCamera.transform.position, nodeInfo.NodeTransform.position);
                nodeInfo.UpdateDistance(distance);

                // Calculate and update detail level based on distance and zoom
                DetailLevel newDetailLevel = CalculateDetailLevel(distance, currentZoomFactor);
                nodeInfo.UpdateDetailLevel(newDetailLevel);
            }

            // Advance batch index for next update cycle
            updateBatchIndex = (updateBatchIndex + nodesToUpdate) % Mathf.Max(cachedNodes.Count, 1);
        }

        /// <summary>
        /// Calculates appropriate detail level from distance and zoom factor.
        /// Uses a two-factor decision matrix: closer distance OR higher zoom = more detail.
        /// </summary>
        private DetailLevel CalculateDetailLevel(float distance, float zoomFactor)
        {
            // Very close and high zoom = Full detail
            if (distance <= veryCloseDistance && zoomFactor >= highZoomThreshold)
            {
                return DetailLevel.Full;
            }

            // Close distance or high zoom = Medium detail
            if (distance <= closeDistance || zoomFactor >= highZoomThreshold)
            {
                return DetailLevel.Medium;
            }

            // Medium distance or medium zoom = Basic detail
            if (distance <= mediumDistance || zoomFactor >= mediumZoomThreshold)
            {
                return DetailLevel.Basic;
            }

            // Far distance and low zoom = Overview only
            return DetailLevel.Overview;
        }

        private void OnDrawGizmos()
        {
            if (!isInitialized || mainCamera == null)
            {
                return;
            }

            if (showDistanceGizmos)
            {
                Vector3 cameraPos = mainCamera.transform.position;

                Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
                Gizmos.DrawWireSphere(cameraPos, veryCloseDistance);

                Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
                Gizmos.DrawWireSphere(cameraPos, closeDistance);

                Gizmos.color = new Color(1f, 0.6f, 0f, 0.2f);
                Gizmos.DrawWireSphere(cameraPos, mediumDistance);

                Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
                Gizmos.DrawWireSphere(cameraPos, farDistance);
            }

            if (showClosestNodeLines)
            {
                DrawClosestNodeLines();
            }
        }

        private void DrawClosestNodeLines()
        {
            if (cachedNodes.Count == 0)
            {
                return;
            }

            Vector3 cameraPos = mainCamera.transform.position;
            List<CachedNodeInfo> sorted = new List<CachedNodeInfo>(cachedNodes);
            sorted.Sort((a, b) => a.LastKnownDistance.CompareTo(b.LastKnownDistance));

            for (int i = 0; i < Mathf.Min(5, sorted.Count); i++)
            {
                if (sorted[i].NodeTransform == null)
                {
                    continue;
                }

                float normalizedDist = Mathf.Clamp01(sorted[i].LastKnownDistance / farDistance);
                Gizmos.color = Color.Lerp(Color.green, Color.red, normalizedDist);
                Gizmos.DrawLine(cameraPos, sorted[i].NodeTransform.position);
            }
        }

        private void OnGUI()
        {
            if (!debugMode || !isInitialized)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(10, 10, 400, 250));
            GUILayout.Box($"Semantic Zoom - {gameObject.name}");
            GUILayout.Label($"Nodes: {cachedNodes.Count}");
            GUILayout.Label($"Zoom: {currentZoomFactor:F2}x");
            GUILayout.Label($"Camera: {mainCamera.transform.position:F1}");

            if (cachedNodes.Count > 0)
            {
                CachedNodeInfo closest = FindClosestNode();
                if (closest != null && closest.NodeGameObject != null)
                {
                    GUILayout.Space(5);
                    GUILayout.Label($"<b>Closest Node:</b>");
                    GUILayout.Label($"  Name: {closest.NodeGameObject.name}");
                    GUILayout.Label($"  Distance: {closest.LastKnownDistance:F2}");
                    GUILayout.Label($"  Zone: {GetDistanceZoneName(closest.LastKnownDistance)}");
                    GUILayout.Label($"  Detail: {closest.CurrentDetailLevel}");
                }

                // Count nodes at each detail level
                int overview = 0, basic = 0, medium = 0, full = 0;
                foreach (var node in cachedNodes)
                {
                    switch (node.CurrentDetailLevel)
                    {
                        case DetailLevel.Overview: overview++; break;
                        case DetailLevel.Basic: basic++; break;
                        case DetailLevel.Medium: medium++; break;
                        case DetailLevel.Full: full++; break;
                    }
                }
                GUILayout.Space(5);
                GUILayout.Label($"<b>Detail Levels:</b>");
                GUILayout.Label($"  Overview: {overview} | Basic: {basic}");
                GUILayout.Label($"  Medium: {medium} | Full: {full}");
            }

            GUILayout.EndArea();
        }

        private CachedNodeInfo FindClosestNode()
        {
            CachedNodeInfo closest = null;
            float minDistance = float.MaxValue;

            foreach (CachedNodeInfo node in cachedNodes)
            {
                if (node.NodeTransform != null && node.LastKnownDistance < minDistance)
                {
                    minDistance = node.LastKnownDistance;
                    closest = node;
                }
            }

            return closest;
        }

        private string GetDistanceZoneName(float distance)
        {
            if (distance <= veryCloseDistance) return "Very Close";
            if (distance <= closeDistance) return "Close";
            if (distance <= mediumDistance) return "Medium";
            if (distance <= farDistance) return "Far";
            return "Very Far";
        }

        /// <summary>
        /// Gets cached information for a specific node GameObject.
        /// </summary>
        public CachedNodeInfo GetNodeInfo(GameObject nodeGameObject)
        {
            if (nodeGameObject == null)
            {
                return null;
            }

            return cachedNodes.Find(n => n.NodeGameObject == nodeGameObject);
        }

        /// <summary>
        /// Gets all tracked nodes in this city.
        /// </summary>
        public IReadOnlyList<CachedNodeInfo> GetAllNodes()
        {
            return cachedNodes.AsReadOnly();
        }

        /// <summary>
        /// Gets the current zoom factor for this city (1.0 = no zoom, higher = zoomed in).
        /// </summary>
        public float GetCurrentZoomFactor()
        {
            return currentZoomFactor;
        }

        /// <summary>
        /// Manually refreshes the node list. Use if nodes are added/removed at runtime.
        /// </summary>
        public void RefreshNodes()
        {
            DiscoverNodes();
        }
    }

    /// <summary>
    /// Cached information about a node for efficient distance tracking.
    /// Avoids repeated GetComponent calls and hierarchy traversal.
    /// </summary>
    public class CachedNodeInfo
    {
        public Transform NodeTransform { get; private set; }
        public GameObject NodeGameObject { get; private set; }
        public NodeRef NodeRef { get; private set; }
        public float LastKnownDistance { get; private set; }
        public float LastUpdateTime { get; private set; }
        public DetailLevel CurrentDetailLevel { get; private set; }
        public DetailLevel PreviousDetailLevel { get; private set; }

        // Event fired when detail level changes
        public event Action<CachedNodeInfo, DetailLevel, DetailLevel> OnDetailLevelChanged;

        public CachedNodeInfo(GameObject nodeGameObject, NodeRef nodeRef)
        {
            NodeGameObject = nodeGameObject;
            NodeTransform = nodeGameObject.transform;
            NodeRef = nodeRef;
            LastKnownDistance = float.MaxValue;
            LastUpdateTime = 0f;
            CurrentDetailLevel = DetailLevel.Overview;
            PreviousDetailLevel = DetailLevel.Overview;
        }

        /// <summary>
        /// Updates the stored distance and timestamp. Called by SemanticZoomController.
        /// </summary>
        public void UpdateDistance(float distance)
        {
            LastKnownDistance = distance;
            LastUpdateTime = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// Updates detail level and fires event if it changed.
        /// </summary>
        public void UpdateDetailLevel(DetailLevel newLevel)
        {
            if (newLevel != CurrentDetailLevel)
            {
                PreviousDetailLevel = CurrentDetailLevel;
                CurrentDetailLevel = newLevel;
                OnDetailLevelChanged?.Invoke(this, PreviousDetailLevel, CurrentDetailLevel);
            }
        }
    }
}
