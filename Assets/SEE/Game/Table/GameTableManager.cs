using SEE.Game.Drawable;
using SEE.GameObjects;
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
		/// Also it deactivates the <see cref="CitySelectionManager"/> component
		/// because it would be triggered when the table is placed while the user
		/// is clicking the left mouse button.
		/// </summary>
		/// <returns>The spawned table.</returns>
        public static GameObject Spawn()
		{
			GameObject table = PrefabInstantiator.InstantiatePrefab(universalTablePrefab);
			table.name = universalTablePrefix + RandomStrings.GetRandomString(10);
			table.transform.GetComponentInChildren<CitySelectionManager>().enabled = false;
			return table;
		}

		/// <summary>
		/// Finishes the placement of a table and reactivates the <see cref="CitySelectionManager"/>
		/// component.
		/// </summary>
		/// <param name="table">The spawned table.</param>
		public static void FinishSpawn(GameObject table)
		{
            table.transform.GetComponentInChildren<CitySelectionManager>().enabled = true;
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
		/// <returns></returns>
		public static GameObject Respawn(string name, Vector3 position, Vector3 eulerAngles)
		{
			GameObject table = Spawn();
			table.name = name;
			Move(table, position);
			Rotate(table, eulerAngles);
			FinishSpawn(table);
			return table;
		}
	}
}