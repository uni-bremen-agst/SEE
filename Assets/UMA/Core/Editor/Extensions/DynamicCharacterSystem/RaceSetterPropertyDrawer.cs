#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace UMA.CharacterSystem.Editors
{
	[CustomPropertyDrawer(typeof(DynamicCharacterAvatar.RaceSetter))]
	public class RaceSetterPropertyDrawer : PropertyDrawer
	{

		public DynamicCharacterAvatar thisDCA;
		//public DynamicRaceLibrary thisDynamicRaceLibrary;
		//In the Editor when the app is NOT running this shows all the races you COULD choose- including those AssetBundles.
		//When the app IS running it shows the reaces you CAN choose- i.e. the ones that are either in the build or have been downloaded.
		public List<RaceData> foundRaces = new List<RaceData>();
		public List<string> foundRaceNames = new List<string>();

		public void SetRaceLists(RaceData[] raceDataArray = null)
		{
			foundRaces.Clear();
			foundRaceNames.Clear();
			foundRaces.Add(null);
			foundRaceNames.Add("None Set");
			if (raceDataArray == null)
            {
				return;
            }
			foreach (RaceData race in raceDataArray)
			{
				if (race != null && race.raceName != "RaceDataPlaceholder")
				{
					foundRaces.Add(race);
					foundRaceNames.Add(race.raceName);
				}
			}
		}
		private void CheckRaceDataLists()
		{
			if (UMAContext.Instance == null)
            {
				var raceDatas = UMAAssetIndexer.Instance.GetAllAssets<RaceData>();
				SetRaceLists(raceDatas.ToArray());
				return;
            }
			if (Application.isPlaying)
			{
				//Start will have cleared any EditorAdded Assets and we only *need* the ones in the library
				var raceDatas = UMAContext.Instance.GetAllRacesBase();
				SetRaceLists(raceDatas);
			}
			else
			{
					var raceDatas = UMAContext.Instance.GetAllRaces();
					if ((raceDatas.Length + 1) != (foundRaces.Count))
					{
						SetRaceLists(raceDatas);
					}
			}
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			CheckRaceDataLists();

			var RaceName = property.FindPropertyRelative("name");
			
			string rn = RaceName.stringValue;
			int rIndex = 0;
			int newrIndex;
			if (rn != "")
			{
				if (!foundRaceNames.Contains(rn))
				{
					foundRaceNames.Add(rn + " (Not Available)");
					foundRaces.Add(null);
				}
				rIndex = foundRaceNames.IndexOf(rn) == -1 ? (foundRaceNames.IndexOf(rn + " (Not Available)") == -1 ? 0 : foundRaceNames.IndexOf(rn + " (Not Available)")) : foundRaceNames.IndexOf(rn);
			}
			EditorGUI.BeginProperty(position, label, property);
			Rect contentPosition = EditorGUI.PrefixLabel(position, new GUIContent("Active Race"));
			Rect contentPositionP = contentPosition;
			EditorGUI.BeginChangeCheck();
			newrIndex = EditorGUI.Popup(contentPositionP, rIndex, foundRaceNames.ToArray());
			if (EditorGUI.EndChangeCheck())
			{
				if (rIndex != newrIndex)
				{
					RaceName.stringValue = foundRaceNames[newrIndex];
					//somehow if the app is playing this already works- and doing it here makes it not work
					if (!EditorApplication.isPlaying)
						property.serializedObject.ApplyModifiedProperties();
				}
			}

			EditorGUI.EndProperty();
		}
	}
}
#endif
