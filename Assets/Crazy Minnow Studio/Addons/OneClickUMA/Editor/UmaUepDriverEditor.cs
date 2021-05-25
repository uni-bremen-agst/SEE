using UnityEditor;
using UnityEngine;

namespace CrazyMinnow.SALSA.OneClicks
{
	[CustomEditor(typeof(UmaUepDriver))]
	public class UmaUepDriverEditor : Editor
	{
		private UmaUepDriver uepDriver;
		private GUIStyle stylewrap = new GUIStyle();

		private void OnEnable()
		{
			uepDriver = target as UmaUepDriver;
			uepDriver.uepProxy = uepDriver.GetComponent<UmaUepProxy>();
		}

		public override void OnInspectorGUI()
		{
			if (uepDriver.uepProxy.isPreviewing)
				EnablePreview();
			else
				DisablePreview();

			uepDriver.isDynamic = GUILayout.Toggle(uepDriver.isDynamic,
				new GUIContent("UMA Character is Dynamic",
					"Leave this enabled for dynamic UMA character avatars."));


			GUILayout.BeginVertical(EditorStyles.helpBox);
				GUILayout.BeginHorizontal();
					GUILayout.BeginVertical(GUILayout.MaxHeight(5f));
						GUILayout.FlexibleSpace();
						GUILayout.Label("Options for Eyes Module:");

						GUILayout.BeginHorizontal();
							GUILayout.Space(15f);
							uepDriver.useHead = GUILayout.Toggle(uepDriver.useHead,
								new GUIContent("Use Head", "Enable to leverage OneClick setup for Head."));
						GUILayout.EndHorizontal();

						GUILayout.BeginHorizontal();
							GUILayout.Space(15f);
							uepDriver.useEyes = GUILayout.Toggle(uepDriver.useEyes,
								new GUIContent("Use Eyes", "Enable to leverage OneClick setup for eyes."));
						GUILayout.EndHorizontal();

						GUILayout.FlexibleSpace();
					GUILayout.EndVertical();

					InspectorCommon.DrawBackgroundCondition(InspectorCommon.AlertType.Warning);
					GUILayout.BeginVertical(GUILayout.MaxHeight(50f));
						GUILayout.FlexibleSpace();
						stylewrap.wordWrap = true;
						stylewrap.fontStyle = FontStyle.Bold;
						stylewrap.alignment = TextAnchor.MiddleCenter;
						GUILayout.BeginHorizontal(EditorStyles.helpBox);
						GUILayout.Label("Leave this component open to enable preview mode for UMA.", stylewrap);
						GUILayout.EndHorizontal();
						GUILayout.FlexibleSpace();
					GUILayout.EndVertical();
					InspectorCommon.DrawResetBg();
				GUILayout.EndHorizontal();
			GUILayout.EndVertical();

		}

		private void DisablePreview()
		{
			uepDriver.isPreview = false;
			if (uepDriver.previewComponent != null)
			{
				float[] zeroes = new float[uepDriver.expPlayer.Values.Length];
				uepDriver.expPlayer.Values = zeroes;
				EditorUtility.SetDirty(uepDriver.expPlayer);
				AssetDatabase.SaveAssets();
				uepDriver.expPlayer.Initialize();

				if (uepDriver.previewComponent.twirler != null)
					DestroyImmediate(uepDriver.previewComponent.twirler.gameObject);
				DestroyImmediate(uepDriver.expPlayer);
				DestroyImmediate(uepDriver.previewComponent);
			}
		}

		private void EnablePreview()
		{
			if (EditorApplication.isPlaying) return;

			if (uepDriver.previewComponent == null)
			{
				uepDriver.isPreview = true;
				uepDriver.previewComponent = uepDriver.gameObject.AddComponent<UmaUepDriverEditorPreview>();
			}
		}
	}
}