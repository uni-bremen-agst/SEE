#if UNITY_EDITOR
using UnityEditor;


namespace InControl
{
	using System;
	using UnityEngine;
	using Internal;


	[CustomEditor( typeof(InControlManager) )]
	public class InControlManagerEditor : Editor
	{
		SerializedProperty logDebugInfo;
		SerializedProperty invertYAxis;
		SerializedProperty useFixedUpdate;
		SerializedProperty dontDestroyOnLoad;
		SerializedProperty suspendInBackground;
		SerializedProperty updateMode;

		SerializedProperty enableICade;

		SerializedProperty enableXInput;
		SerializedProperty xInputOverrideUpdateRate;
		SerializedProperty xInputUpdateRate;
		SerializedProperty xInputOverrideBufferSize;
		SerializedProperty xInputBufferSize;

		SerializedProperty enableNativeInput;
		SerializedProperty nativeInputEnableXInput;
		SerializedProperty nativeInputEnableMFi;
		SerializedProperty nativeInputPreventSleep;
		SerializedProperty nativeInputOverrideUpdateRate;
		SerializedProperty nativeInputUpdateRate;

		Texture headerTexture;


		void OnEnable()
		{
			logDebugInfo = serializedObject.FindProperty( "logDebugInfo" );
			invertYAxis = serializedObject.FindProperty( "invertYAxis" );
			useFixedUpdate = serializedObject.FindProperty( "useFixedUpdate" );
			dontDestroyOnLoad = serializedObject.FindProperty( "dontDestroyOnLoad" );
			suspendInBackground = serializedObject.FindProperty( "suspendInBackground" );
			updateMode = serializedObject.FindProperty( "updateMode" );

			enableICade = serializedObject.FindProperty( "enableICade" );

			enableXInput = serializedObject.FindProperty( "enableXInput" );
			xInputOverrideUpdateRate = serializedObject.FindProperty( "xInputOverrideUpdateRate" );
			xInputUpdateRate = serializedObject.FindProperty( "xInputUpdateRate" );
			xInputOverrideBufferSize = serializedObject.FindProperty( "xInputOverrideBufferSize" );
			xInputBufferSize = serializedObject.FindProperty( "xInputBufferSize" );

			enableNativeInput = serializedObject.FindProperty( "enableNativeInput" );
			nativeInputEnableXInput = serializedObject.FindProperty( "nativeInputEnableXInput" );
			nativeInputEnableMFi = serializedObject.FindProperty( "nativeInputEnableMFi" );
			nativeInputPreventSleep = serializedObject.FindProperty( "nativeInputPreventSleep" );
			nativeInputOverrideUpdateRate = serializedObject.FindProperty( "nativeInputOverrideUpdateRate" );
			nativeInputUpdateRate = serializedObject.FindProperty( "nativeInputUpdateRate" );

			headerTexture = EditorTextures.InControlHeader;
		}


		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			GUILayout.Space( 5.0f );

			var headerRect = GUILayoutUtility.GetRect( 0.0f, 5.0f );
			headerRect.width = headerTexture.width / 2;
			headerRect.height = headerTexture.height / 2;
			GUILayout.Space( headerRect.height );
			GUI.DrawTexture( headerRect, headerTexture );

			EditorUtility.SetTintColor();
			var versionStyle = new GUIStyle( EditorUtility.wellStyle );
			versionStyle.alignment = TextAnchor.MiddleCenter;
			GUILayout.Box( "Version " + InputManager.Version, versionStyle, GUILayout.ExpandWidth( true ) );
			EditorUtility.PopTintColor();

			EditorUtility.BeginGroup( "General Settings" );

			logDebugInfo.boolValue = EditorGUILayout.ToggleLeft( "Log Debug Info", logDebugInfo.boolValue, EditorUtility.labelStyle );
			invertYAxis.boolValue = EditorGUILayout.ToggleLeft( "Invert Y Axis", invertYAxis.boolValue, EditorUtility.labelStyle );

			// This is deprecated and replaced by updateMode
			// useFixedUpdate.boolValue = EditorGUILayout.ToggleLeft( "Use Fixed Update", useFixedUpdate.boolValue, EditorUtility.labelStyle );

			dontDestroyOnLoad.boolValue = EditorGUILayout.ToggleLeft( "Don't Destroy On Load", dontDestroyOnLoad.boolValue, EditorUtility.labelStyle );
			suspendInBackground.boolValue = EditorGUILayout.ToggleLeft( "Suspend In Background", suspendInBackground.boolValue, EditorUtility.labelStyle );

			GUILayout.Space( 5.0f );
			var rect = EditorGUILayout.GetControlRect( false, 1 );
			rect.height = 1;
			EditorGUI.DrawRect( rect, EditorGUIUtility.isProSkin ? new Color( 1, 1, 1, 0.2f ) : new Color( 0, 0, 0, 0.2f ) );
			GUILayout.Space( 5.0f );

			var selectedUpdateMode = (InControlUpdateMode) Enum.GetValues( typeof(InControlUpdateMode) ).GetValue( updateMode.enumValueIndex );
			if (useFixedUpdate.boolValue)
			{
				selectedUpdateMode = InControlUpdateMode.FixedUpdate;
				useFixedUpdate.boolValue = false;
			}

			updateMode.enumValueIndex = (int) (InControlUpdateMode) EditorGUILayout.EnumPopup( "Update Mode", selectedUpdateMode );
			// EditorGUILayout.PropertyField( updateMode );

			EditorUtility.EndGroup();


			EditorUtility.GroupTitle( "Enable ICade (iOS/tvOS only)", enableICade );


			EditorUtility.GroupTitle( "Enable XInput (Windows only)", enableXInput );
			if (enableXInput.boolValue)
			{
				EditorUtility.BeginGroup();

				//var text = "" +
				//		   "<b>Warning: <color=#cc0000>Advanced Settings</color></b>\n" +
				//		   "Do not modify these unless you perfectly understand what effect they will have. " +
				//		   "Set to zero to automatically use sensible defaults.";
				//GUILayout.Box( text, EditorUtility.wellStyle, GUILayout.ExpandWidth( true ) );

				xInputOverrideUpdateRate.boolValue = EditorGUILayout.ToggleLeft( "Override Update Rate <color=#777>(Not Recommended)</color>", xInputOverrideUpdateRate.boolValue, EditorUtility.labelStyle );
				xInputUpdateRate.intValue = xInputOverrideUpdateRate.boolValue ? Mathf.Max( EditorGUILayout.IntField( "Update Rate (Hz)", xInputUpdateRate.intValue ), 0 ) : 0;

				xInputOverrideBufferSize.boolValue = EditorGUILayout.ToggleLeft( "Override Buffer Size <color=#777>(Not Recommended)</color>", xInputOverrideBufferSize.boolValue, EditorUtility.labelStyle );
				xInputBufferSize.intValue = xInputOverrideBufferSize.boolValue ? Mathf.Max( xInputBufferSize.intValue, EditorGUILayout.IntField( "Buffer Size", xInputBufferSize.intValue ), 0 ) : 0;

				EditorUtility.EndGroup();
			}


			EditorUtility.GroupTitle( "Enable Native Input (Windows/macOS/iOS/tvOS only)", enableNativeInput );
			if (enableNativeInput.boolValue)
			{
				EditorUtility.BeginGroup();

				var text1 = "" +
				            //							"<b>Warning: <color=#cc0000>This feature is in BETA!</color></b>\n" +
				            "Enabling native input will disable using Unity input internally, " +
				            "but should provide more efficient and robust input support.";
				EditorUtility.SetTintColor();
				GUILayout.Box( text1, EditorUtility.wellStyle, GUILayout.ExpandWidth( true ) );
				EditorUtility.PopTintColor();

				nativeInputEnableXInput.boolValue = EditorGUILayout.ToggleLeft( "Enable XInput Support <color=#777>(Windows Only)</color>", nativeInputEnableXInput.boolValue, EditorUtility.labelStyle );
				nativeInputEnableMFi.boolValue = EditorGUILayout.ToggleLeft( "Enable MFi Support On macOS <color=#cc5500>(BETA)</color>", nativeInputEnableMFi.boolValue, EditorUtility.labelStyle );
				nativeInputPreventSleep.boolValue = EditorGUILayout.ToggleLeft( "Prevent Screensaver / Sleep", nativeInputPreventSleep.boolValue, EditorUtility.labelStyle );

				//var text2 = "" +
				//			"<b>Warning: <color=#cc0000>Advanced Settings</color></b>\n" +
				//			"Do not modify these unless you perfectly understand what effect they will have. " +
				//			"Set to zero to automatically use sensible defaults.";
				//GUILayout.Box( text2, EditorUtility.wellStyle, GUILayout.ExpandWidth( true ) );

				nativeInputOverrideUpdateRate.boolValue = EditorGUILayout.ToggleLeft( "Override Update Rate <color=#777>(Not Recommended)</color>", nativeInputOverrideUpdateRate.boolValue, EditorUtility.labelStyle );
				nativeInputUpdateRate.intValue = nativeInputOverrideUpdateRate.boolValue ? Mathf.Max( nativeInputUpdateRate.intValue, EditorGUILayout.IntField( "Update Rate (Hz)", nativeInputUpdateRate.intValue ), 0 ) : 0;

				EditorUtility.EndGroup();
			}

			GUILayout.Space( 10.0f );

			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif
