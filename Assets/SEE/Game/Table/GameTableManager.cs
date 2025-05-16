using HighlightPlus;
using MoreLinq;
using SEE.DataModel.DG;
using SEE.Game.City;
using SEE.GameObjects;
using SEE.GO;
using SEE.Utils;
using System.Linq;
using UnityEngine;

namespace SEE.Game.Table
{
    /// <summary>
    /// This class manages the tables modification.
    /// </summary>
    public static class GameTableManager
    {
        /// <summary>
        /// The prefab of the universal table.
        /// </summary>
        private const string universalTablePrefab = "Prefabs/Table/UniversalTable";

        /// <summary>
        /// The name prefix of an universal table.
        /// </summary>
        private const string universalTablePrefix = "UniversalTable";

        /// <summary>
        /// The standard height of a table.
        /// </summary>
        private const float standardTableHeight = 0.4470662f;

        /// <summary>
        /// Spawns a new universal table with a randomized name.
        /// Also, it deactivates the <see cref="CitySelectionManager"/> component
        /// because it would be triggered when the table is placed while the user
        /// is clicking the left mouse button.
        /// Additionally, collision detection will be actived to ensure that the
        /// table does not overlap with another object.
        /// </summary>
        /// <param name="name">The name for the table. If null, a randomized name will be used.</param>
        /// <returns>The spawned table.</returns>
        public static GameObject Spawn(string name = null)
        {
            GameObject table = PrefabInstantiator.InstantiatePrefab(universalTablePrefab);
            table.name = name ?? universalTablePrefix + RandomStrings.GetRandomString(10);
            table.transform.GetComponentInChildren<CitySelectionManager>().enabled = false;
            EnableEditMode(table);
            return table;
        }

        /// <summary>
        /// Finishes the placement of a table and reactivates the <see cref="CitySelectionManager"/>
        /// component.
        /// Additionally, collision detection will be removed.
        /// </summary>
        /// <param name="table">The spawned table.</param>
        public static void FinishSpawn(GameObject table)
        {
            table.transform.GetComponentInChildren<CitySelectionManager>().enabled = true;
            DisableEditMode(table);
        }

        /// <summary>
        /// Moves the specified <paramref name="table"/> to the specified <paramref name="position"/>.
        /// </summary>
        /// <param name="table">The table to move.</param>
        /// <param name="position">The position where the table should be placed.</param>
        public static void Move(GameObject table, Vector3 position)
        {
            table.transform.position = new Vector3(position.x, standardTableHeight, position.z);
        }

        /// <summary>
        /// Moves the specified <paramref name="table"/> to the specified <paramref name="position"/>,
        /// including the associated portal, if the city is drawn.
        /// </summary>
        /// <param name="table">The table to move.</param>
        /// <param name="position">The position where the table should be placed.</param>
        public static void MoveIncPortal(GameObject table, Vector3 position)
        {
            table.transform.position = new Vector3(position.x, standardTableHeight, position.z);
            if (table.GetComponentInChildren<AbstractSEECity>() is { } city
                && city.gameObject.IsCodeCityDrawn())
            {
                Portal.SetPortal(city.gameObject);
            }
        }

        /// <summary>
        /// Scales the specified <paramref name="table"/> to the specified <paramref name="scale"/>.
        /// </summary>
        /// <param name="table">The table to scale.</param>
        /// <param name="scale">The scale where the table should be scaled.</param>
        public static void Scale(GameObject table, Vector3 scale)
        {
            GameObject city = table.FindDescendantWithTag(Tags.CodeCity);

            if (!city.IsCodeCityDrawn())
            {
                table.transform.localScale = scale;
                return;
            }

            Transform root = city.transform.Cast<Transform>().First(child => child.gameObject.IsNode());
            Node rootNode = root.gameObject.GetNode();
            bool isReflexion = city.GetComponent<SEEReflexionCity>() != null;

            DetachChildren(rootNode, isReflexion);
            table.transform.localScale = scale;
            Portal.SetPortal(city);
            ReattachChildren(rootNode, root, isReflexion);

            void DetachChildren(Node rootNode, bool isReflexion)
            {
                if (isReflexion)
                {
                    rootNode.Children().ForEach(subroot =>
                        subroot.Children().ForEach(n => n.GameObject().transform.parent = null));
                }
                else
                {
                    rootNode.Children().ForEach(n => n.GameObject().transform.parent = null);
                }
            }

            void ReattachChildren(Node rootNode, Transform rootTransform, bool isReflexion)
            {
                if (isReflexion)
                {
                    rootNode.Children().ForEach(subroot =>
                        subroot.Children().ForEach(n => n.GameObject().transform.parent = subroot.GameObject().transform));
                }
                else
                {
                    rootNode.Children().ForEach(n => n.GameObject().transform.parent = root);
                }
            }
        }

        /// <summary>
        /// Checks whether the table can be scaled down.
        /// A table cannot be scaled down if any node would be cut off.
        /// </summary>
        /// <param name="table">The table to be scaled.</param>
        /// <param name="scale">The new scale of the table.</param>
        /// <returns>True if the table can be scaled down; otherwise, false.</returns>
        public static bool CanScaleDown(GameObject table, Vector3 scale)
        {
            SEEReflexionCity city = table.GetComponentInChildren<SEEReflexionCity>();
            if (city == null || !city.gameObject.IsCodeCityDrawnAndActive())
            {
                return true;
            }
            Node archRoot = city.ReflexionGraph.GetNode(city.ReflexionGraph.ArchitectureRoot.ID);
            Transform archTrans = archRoot.GameObject().transform;
            Vector3 oTableScale = table.transform.localScale;

            archRoot.Children().ForEach(child => child.GameObject().transform.SetParent(null, true));
            table.transform.localScale = scale;

            Bounds newBounds = GetCombinedBounds(archRoot.GameObject());
            foreach (Node n in archRoot.Children())
            {
                Transform trans = n.GameObject().transform;
                Renderer r = trans.GetComponent<Renderer>();
                if (r != null && !AreCornersInsideXZ(r.bounds, newBounds))
                {
                    table.transform.localScale = oTableScale;
                    foreach (Node no in archRoot.Children())
                    {
                        no.GameObject().transform.SetParent(archTrans);
                    }
                    return false;
                }
            }

            foreach (Node n in archRoot.Children())
            {
                n.GameObject().transform.SetParent(archTrans, true);
            }

            return true;
            Bounds GetCombinedBounds(GameObject obj)
            {
                Renderer[] renderers = obj.GetComponents<Renderer>();
                if (renderers.Length == 0)
                {
                    return new Bounds(obj.transform.position, Vector3.zero);
                }

                Bounds combined = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    combined.Encapsulate(renderers[i].bounds);
                }

                return combined;
            }
            bool AreCornersInsideXZ(Bounds childBounds, Bounds parentBounds)
            {
                Vector3[] corners = new Vector3[4];

                Vector3 min = childBounds.min;
                Vector3 max = childBounds.max;

                corners[0] = new Vector3(min.x, min.y, min.z);
                corners[1] = new Vector3(min.x, min.y, max.z);
                corners[2] = new Vector3(max.x, min.y, min.z);
                corners[3] = new Vector3(max.x, min.y, max.z);

                foreach (Vector3 corner in corners)
                {
                    if (!IsInsideXZ(corner, parentBounds))
                        return false;
                }

                return true;
            }
            bool IsInsideXZ(Vector3 point, Bounds bounds)
            {
                return point.x >= bounds.min.x && point.x <= bounds.max.x &&
                       point.z >= bounds.min.z && point.z <= bounds.max.z;
            }
        }

        /// <summary>
        /// Respawns a table with the given <paramref name="name"/>,
        /// at the specified <paramref name="position"/>,
        /// and with the provided <paramref name="eulerAngles"/>.
        /// </summary>
        /// <param name="name">The table name.</param>
        /// <param name="position">The position of the table.</param>
        /// <param name="scale">The scale of the table.</param>
        /// <returns>The spawned table.</returns>
        public static GameObject Respawn(string name, Vector3 position, Vector3 scale)
        {
            GameObject table = Spawn(name);
            Move(table, position);
            Scale(table, scale);
            FinishSpawn(table);
            return table;
        }

        /// <summary>
        /// Destroyes the given <paramref name="table"/>.
        /// Also removes the table from <see cref="CitiesHolder.Cities"/>.
        /// </summary>
        /// <param name="table">The table to destroy.</param>
        public static void Destroy(GameObject table)
        {
            if (!table.HasDescendantWithTag(Tags.CodeCity))
            {
                return;
            }
            if (LocalPlayer.TryGetCitiesHolder(out CitiesHolder citiesHolder))
            {
                citiesHolder.Cities.Remove(table.name);
            }
            Destroyer.Destroy(table);
        }

        /// <summary>
        /// Enables and adds a <see cref="HighlightEffect"/> on the given <paramref name="table"/>
        /// if it doesn't already exist, then configures and activates the highlight effect.
        /// </summary>
        /// <param name="table">The table to which the <see cref="HighlightEffect"/> should be added.</param>
        private static void ActivateHighlight(GameObject table)
        {
            if (table.GetComponent<HighlightEffect>() == null)
            {
                table.AddComponent<HighlightEffect>();
            }
            HighlightEffect effect = table.GetComponent<HighlightEffect>();
            effect.enabled = true;
            effect.highlighted = true;
            effect.outlineColor = Color.yellow;
            effect.outlineWidth = 0.3f;
            effect.effectGroup = TargetOptions.OnlyThisObject;
        }

        /// <summary>
        /// Destroys the <see cref="HighlightEffect"/> component attached to the given <paramref name="table"/>.
        /// </summary>
        /// <param name="table">The table whose <see cref="HighlightEffect"/> should be removed.</param>
        private static void DestroyHighlight(GameObject table)
        {
            if (table.GetComponent<HighlightEffect>() != null)
            {
                Destroyer.Destroy(table.GetComponent<HighlightEffect>());
            }
        }

        /// <summary>
        /// Enables the edit mode for the given <paramref name="table"/>.
        /// Adds a <see cref="CollisionDetectionManager"/> to detect collisions and
        /// activates a <see cref="HighlightEffect"/>.
        /// </summary>
        /// <param name="table">The table whose edit mode should be enabled.</param>
        public static void EnableEditMode(GameObject table)
        {
            if (table.GetComponent<CollisionDetectionManager>() == null)
            {
                table.AddComponent<CollisionDetectionManager>();
            }
            ActivateHighlight(table);
        }

        /// <summary>
        /// Disables the edit mode of the given <paramref name="table"/>
        /// by destroying the dependent components.
        /// </summary>
        /// <param name="table">The table whose edit mode should be disabled.</param>
        public static void DisableEditMode(GameObject table)
        {
            if (table.GetComponent<CollisionDetectionManager>() != null)
            {
                Destroyer.Destroy(table.GetComponent<CollisionDetectionManager>());
            }
            DestroyHighlight(table);
        }

        /// <summary>
        /// Disables the active associated <see cref="CitySelectionManager"/>.
        /// </summary>
		/// <param name="table">The table whose <see cref="CitySelectionManager"/> should be disabled.</param>
        public static void DisableCSM(GameObject table)
        {
            if (table.FindDescendantWithTag(Tags.CodeCity) is GameObject city)
            {
                city.GetComponent<CitySelectionManager>().enabled = false;
            }
        }

        /// <summary>
        /// Enables the city associated with the specified table by redrawing it,
        /// if it has already been drawn, or enabling the associated <see cref="CitySelectionManager"/>.
        /// </summary>
        /// <param name="table">The table whose city needs to be redrawn.</param>
        public static void EnableCity(GameObject table)
        {
            if (table.FindDescendantWithTag(Tags.CodeCity) is GameObject city)
            {
                if (city.IsCodeCityDrawn())
                {
                    city.GetComponent<SEECity>().ReDrawGraph();
                }
                else
                {
                    city.GetComponent<CitySelectionManager>().enabled = true;
                }
            }
        }
    }
}