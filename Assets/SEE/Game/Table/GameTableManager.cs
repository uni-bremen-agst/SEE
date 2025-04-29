using HighlightPlus;
using SEE.Game.City;
using SEE.Game.Drawable;
using SEE.GameObjects;
using SEE.GO;
using SEE.Net.Actions.Drawable;
using SEE.Utils;
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
        /// Roates the specified <paramref name="table"/> by a factor based on <paramref name="scrollDown"/>.
        /// </summary>
        /// <param name="table">The table to roate.</param>
        /// <param name="scrollDown">Indicator whether scrolling down was used.
        /// If true, the rotation is changed by -1; otherwise, it changed by 1.</param>
        public static void Rotate(GameObject table, bool scrollDown)
        {
            float rotateFactor = scrollDown ? -1f : 1f;
            table.transform.eulerAngles -= new Vector3(0, rotateFactor, 0);
        }

        /// <summary>
        /// Rotates the specified <paramref name="table"/> to the specified <paramref name="eulerAngles"/>.
        /// </summary>
        /// <param name="table">The table to rotate.</param>
        /// <param name="eulerAngles">The euler angles where the table should be rotated.</param>
        public static void Rotate(GameObject table, Vector3 eulerAngles)
        {
            table.transform.eulerAngles = eulerAngles;
        }

        /// <summary>
        /// Respawns a table with the given <paramref name="name"/>,
        /// at the specified <paramref name="position"/>,
        /// and with the provided <paramref name="eulerAngles"/>.
        /// </summary>
        /// <param name="name">The table name.</param>
        /// <param name="position">The position of the table.</param>
        /// <param name="eulerAngles">The euler angles of the table.</param>
        /// <returns>The spawned table.</returns>
        public static GameObject Respawn(string name, Vector3 position, Vector3 eulerAngles)
        {
            GameObject table = Spawn(name);
            Move(table, position);
            Rotate(table, eulerAngles);
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
            if (!GameFinder.HasChildWithTag(table, Tags.CodeCity))
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
        /// Disables the active, drawn city or its associated <see cref="CitySelectionManager"/>.
        /// </summary>
		/// <param name="table">The table whose city should be disabled.</param>
        public static void DisableCity(GameObject table)
        {
            if (GameFinder.FindChildWithTag(table, Tags.CodeCity) is GameObject city)
            {
                if (city.IsCodeCityDrawnAndActive()) {
                    GameFinder.FindChildWithTag(city, Tags.Node).SetActive(false);
                }
                else
                {
                    city.GetComponent<CitySelectionManager>().enabled = false;
                }
            }
        }

        /// <summary>
        /// Enables the city associated with the specified table by redrawing it,
        /// if it has already been drawn, or enabling the associated <see cref="CitySelectionManager"/>.
        /// </summary>
        /// <param name="table">The table whose city needs to be redrawn.</param>
        public static void EnableCity(GameObject table)
        {
            if (GameFinder.FindChildWithTag(table, Tags.CodeCity) is GameObject city)
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