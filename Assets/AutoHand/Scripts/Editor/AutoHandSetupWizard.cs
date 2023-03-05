using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Threading;
using Autohand;

[InitializeOnLoad]
public class AutoHandSetupWizard : EditorWindow {
    static AutoHandSetupWizard window;
    static string[] requiredLayerNames = { "Grabbing", "Grabbable", "Hand", "HandPlayer" };
    static string assetPath;

    public static float quality = 2;

    static AutoHandSettings _handSettings = null;
    static AutoHandSettings handSettings {
        get {
            if(_handSettings == null)
                _handSettings = Resources.Load<AutoHandSettings>("AutoHandSettings");
            return _handSettings;
        }
    }

    static AutoHandSetupWizard() {
        EditorApplication.update += Start;
    }


    static void Start() {
        SetRequiredSettings();

        if(ShowSetupWindow()) {
            OpenWindow();
            Application.OpenURL("https://earnest-robot.gitbook.io/auto-hand-docs/");
            assetPath = Application.dataPath;
        }

        EditorApplication.update -= Start;
    }

    [MenuItem("Window/Autohand/Setup Window")]
    public static void OpenWindow() {
        window = GetWindow<AutoHandSetupWizard>(true);
        window.minSize = new Vector2(320, 440);
        window.maxSize = new Vector2(360, 500);
        window.titleContent = new GUIContent("Auto Hand Setup");
        SetRequiredSettings();
    }

    void OnDestroy() {
        SetRequiredSettings();
    }


    public void OnGUI() {

        var rect = EditorGUILayout.GetControlRect();
        rect.height *= 5;
        GUI.Label(rect, (Texture2D)Resources.Load("AutoHandLogo", typeof(Texture2D)), AutoHandExtensions.LabelStyle(TextAnchor.MiddleCenter, FontStyle.Normal, 25));
        rect = EditorGUILayout.GetControlRect();
        rect = EditorGUILayout.GetControlRect();
        rect = EditorGUILayout.GetControlRect();

        GUILayout.Space(12f);
        GUI.Label(qualityLabelRect, "AUTO HAND SETUP", AutoHandExtensions.LabelStyle(TextAnchor.MiddleCenter, FontStyle.Normal, 25));
        GUILayout.Space(12f);

        GUI.Label(qualityLabelRect, "RECOMMENDED PHYSICS SETTINGS", AutoHandExtensions.LabelStyle(TextAnchor.MiddleCenter, FontStyle.Normal, 19));
        GUILayout.Space(24f);

        GUI.Label(qualityLabelRect, "Physics Quality", AutoHandExtensions.LabelStyle(TextAnchor.MiddleCenter, FontStyle.Normal, 16));
        GUILayout.Space(5f);

        quality = GUI.HorizontalSlider(qualitySliderRect, quality, -1, 3);
        quality = Mathf.Round(quality);

        GUI.Label(qualityLabelRect, QualityGUIContent(quality), AutoHandExtensions.LabelStyle(QualityColor(quality), TextAnchor.MiddleCenter));


        ShowQualitySettings(quality);


        GUILayout.Space(30f);
        if(GUI.Button(qualitySliderRect, "Apply")) {
            EditorUtility.SetDirty(handSettings);
            handSettings.quality = quality;
            SetPhysicsSettings(handSettings.quality);
            this.Close();
        }

        ShowDoNotShowButton();
    }


    public void ShowQualitySettings(float quality) {

        GUILayout.Space(15);
        var labelStyle = AutoHandExtensions.LabelStyle(new Color(0.7f, 0.7f, 0.7f, 1f));
        var labelStyleB = AutoHandExtensions.LabelStyleB(new Color(0.7f, 0.7f, 0.7f, 1f));

        handSettings.usingDynamicTimestep = GUI.Toggle(qualitySliderRect, handSettings.usingDynamicTimestep, "Use Dynamic Timestep", labelStyleB);

        if(quality <= -1) {
            GUI.Label(qualitySliderRect, "Ignore Recommended Settings", labelStyle);
        }
        else if(quality <= 0) {
            if(!handSettings.usingDynamicTimestep)
                GUI.Label(qualitySliderRect, "Fixed Timestep: 1/50", labelStyle);
            GUI.Label(qualitySliderRect, "Contact Offset: 0.01", labelStyle);
            GUI.Label(qualitySliderRect, "Solver Iterations: 10", labelStyle);
            GUI.Label(qualitySliderRect, "Solver Velocity Iterations: 5", labelStyle);
        }
        else if(quality <= 1) {
            if(!handSettings.usingDynamicTimestep)
                GUI.Label(qualitySliderRect, "Fixed Timestep: 1/60", labelStyle);
            GUI.Label(qualitySliderRect, "Contact Offset: 0.01", labelStyle);
            GUI.Label(qualitySliderRect, "Solver Iterations: 10", labelStyle);
            GUI.Label(qualitySliderRect, "Solver Velocity Iterations: 5", labelStyle);
            GUI.Label(qualitySliderRect, "Enable Adaptive Force: true", labelStyle);
        }
        else if(quality <= 2) {
            if(!handSettings.usingDynamicTimestep)
                GUI.Label(qualitySliderRect, "Fixed Timestep: 1/72", labelStyle);
            GUI.Label(qualitySliderRect, "Contact Offset: 0.005", labelStyle);
            GUI.Label(qualitySliderRect, "Solver Iterations: 20", labelStyle);
            GUI.Label(qualitySliderRect, "Solver Velocity Iterations: 10", labelStyle);
            GUI.Label(qualitySliderRect, "Enable Adaptive Force: true", labelStyle);
        }
        else if(quality <= 3) {
            if(!handSettings.usingDynamicTimestep)
                GUI.Label(qualitySliderRect, "Fixed Timestep: 1/90", labelStyle);
            GUI.Label(qualitySliderRect, "Contact Offset: 0.0035", labelStyle);
            GUI.Label(qualitySliderRect, "Solver Iterations: 50", labelStyle);
            GUI.Label(qualitySliderRect, "Solver Velocity Iterations: 50", labelStyle);
            GUI.Label(qualitySliderRect, "Enable Adaptive Force: true", labelStyle);
        }

    }

    public static void SetPhysicsSettings(float quality) {

        if(quality <= 0) {
            Time.fixedDeltaTime = 1 / 50f;
            Physics.defaultContactOffset = 0.01f;
            Physics.defaultSolverIterations = 10;
            Physics.defaultSolverVelocityIterations = 5;
            Physics.defaultMaxAngularSpeed = 35f;

        }
        else if(quality <= 1) {
            EnableAdaptiveForce();
            Time.fixedDeltaTime = 1 / 60f;
            Physics.defaultContactOffset = 0.0075f;
            Physics.defaultSolverIterations = 10;
            Physics.defaultSolverVelocityIterations = 5;
            Physics.defaultMaxAngularSpeed = 35f;
        }
        else if(quality <= 2) {
            EnableAdaptiveForce();
            Time.fixedDeltaTime = 1 / 72f;
            Physics.defaultContactOffset = 0.005f;
            Physics.defaultSolverIterations = 20;
            Physics.defaultSolverVelocityIterations = 10;
            Physics.defaultMaxAngularSpeed = 35f;
        }
        else if(quality <= 3) {
            EnableAdaptiveForce();
            Time.fixedDeltaTime = 1 / 90f;
            Physics.defaultContactOffset = 0.0035f;
            Physics.defaultSolverIterations = 30;
            Physics.defaultSolverVelocityIterations = 20;
            Physics.defaultMaxAngularSpeed = 35f;
        }
    }

    public static void SetRequiredSettings() {
        if(!IsGenerated()) {
            GenerateAutoHandLayers();
            UpdateRequiredCollisionLayers();
        }
        if(!IsIgnoreCollisionSet()) {
            UpdateRequiredCollisionLayers();
        }
    }


    void ShowDoNotShowButton() {
        var GUIColor = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.7f, 0.4f, 0.4f);
        GUILayout.Space(30f);
        var buttonRect = new Rect(qualitySliderRect);
        buttonRect.width /= 1.5f;
        buttonRect.height /= 1f;
        buttonRect.x += buttonRect.width / 4f;
        if(GUI.Button(buttonRect, "Dont Show Again")) {
            EditorUtility.SetDirty(handSettings);
            handSettings.ignoreSetup = true;
            this.Close();
        }
        GUI.backgroundColor = GUIColor;
    }


    public Rect qualitySliderRect {
        get {
            var _qualitySlider = EditorGUILayout.GetControlRect();
            _qualitySlider.x += _qualitySlider.width / 6f;
            _qualitySlider.width *= 2 / 3f;
            return _qualitySlider;
        }
    }

    public Rect qualityLabelRect {
        get {
            var _qualitySlider = EditorGUILayout.GetControlRect();
            _qualitySlider.height *= 1.5f;
            return _qualitySlider;
        }
    }

    public Color QualityColor(float quality) {
        if(quality <= 0) {
            return Color.red;
        }
        else if(quality <= 1) {
            return Color.yellow;
        }
        else if(quality <= 2) {
            return Color.green;
        }
        else if(quality <= 3) {
            return Color.magenta;
        }

        return Color.white;
    }


    public GUIContent QualityGUIContent(float quality) {
        var content = new GUIContent();
        if(quality <= -1)
            content.text += "IGNORE RECOMMENDED SETTINGS";
        else if(quality <= 0)
            content.text += "LOW (Not Recommended)";
        else if(quality <= 1)
            content.text += "MEDIUM";
        else if(quality <= 2)
            content.text += "HIGH (Quest Recommended)";
        else if(quality <= 3)
            content.text += "VERY HIGH";

        return content;
    }



    static bool ShowSetupWindow() {
        return handSettings.quality == -1 && !handSettings.ignoreSetup;
    }


    static void GenerateAutoHandLayers() {
        assetPath = Application.dataPath;
        var path = assetPath.Substring(0, assetPath.Length - 6);
        path += "ProjectSettings/TagManager.asset";

        List<string> layerNames = new List<string>();
        for(int i = 0; i < requiredLayerNames.Length; i++) {
            layerNames.Add(requiredLayerNames[i]);
        }

        StreamReader reader = new StreamReader(path);
        string line = reader.ReadLine();
        string[] lines = File.ReadAllLines(path);

        int lineIndex = 0;
        for(lineIndex = 0; lineIndex < lines.Length; lineIndex++) {
            for(int i = 0; i < layerNames.Count; i++) {
                if(lines[lineIndex].Contains(layerNames[i])) {
                    layerNames.RemoveAt(i);
                }
            }
        }

        List<int> lineTargetList = new List<int>();
        lineIndex = 0;
        while((line = reader.ReadLine()) != null) {
            if(line == "  - ") {
                lineTargetList.Add(lineIndex);
            }
            lineIndex++;
        }
        reader.Close();

        var lineTarget = new int[layerNames.Count];
        if(lineTargetList.Count < lineTarget.Length) {
            Debug.LogError("AUTO HAND - SETUP FAILED: Requires 5 available physics layers for automatic setup.");
            return;
        }

        int j = 0;
        for(int i = lineTargetList.Count - 1; j < lineTarget.Length; j++) {
            lineTarget[j] = lineTargetList[i];
            i--;
        }

        StreamWriter writer = new StreamWriter(path);
        lineIndex = 0;
        for(lineIndex = 0; lineIndex < lines.Length; lineIndex++) {
            bool found = false;
            for(int i = 0; i < lineTarget.Length; i++) {
                if(lineIndex == lineTarget[i] + 1) {
                    writer.WriteLine("  - " + layerNames[i]);
                    found = true;
                }
            }
            if(!found)
                writer.WriteLine(lines[lineIndex]);

        }
        Debug.Log("Autohand - Layer setup successful");
        writer.Close();
        AssetDatabase.Refresh();
#if UNITY_2020
#if !UNITY_2020_1
        AssetDatabase.RefreshSettings();
#endif
#endif

    }
    static void EnableAdaptiveForce() {
        assetPath = Application.dataPath;
        var path = assetPath.Substring(0, assetPath.Length - 6);
        path += "ProjectSettings/DynamicsManager.asset";

        List<string> layerNames = new List<string>();
        for(int i = 0; i < requiredLayerNames.Length; i++) {
            layerNames.Add(requiredLayerNames[i]);
        }

        StreamReader reader = new StreamReader(path);
        string line = reader.ReadLine();
        string[] lines = File.ReadAllLines(path);

        int lineIndex = 0;
        List<int> lineTargetList = new List<int>();

        while((line = reader.ReadLine()) != null) {
            if(line.Contains("m_EnableAdaptiveForce"))
                lineTargetList.Add(lineIndex);
            lineIndex++;
        }
        reader.Close();


        StreamWriter writer = new StreamWriter(path);
        lineIndex = 0;
        for(lineIndex = 0; lineIndex < lines.Length; lineIndex++) {
            bool found = false;
            if(lineIndex == lineTargetList[0] + 1) {
                writer.WriteLine("  m_EnableAdaptiveForce: 1");
                found = true;
            }
            if(!found)
                writer.WriteLine(lines[lineIndex]);

        }
        writer.Close();
        AssetDatabase.Refresh();
#if UNITY_2020
#if !UNITY_2020_1
        AssetDatabase.RefreshSettings();
#endif
#endif
    }

    static void UpdateRequiredCollisionLayers() {
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Hand"), LayerMask.NameToLayer("Hand"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Hand"), LayerMask.NameToLayer("Grabbing"), true);

        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("HandPlayer"), LayerMask.NameToLayer("Grabbable"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("HandPlayer"), LayerMask.NameToLayer("Grabbing"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("HandPlayer"), LayerMask.NameToLayer("Hand"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("HandPlayer"), LayerMask.NameToLayer("HandPlayer"), true);
    }


    public static bool IsGenerated() {
        foreach(var layer in requiredLayerNames) {
            if(LayerMask.NameToLayer(layer) == -1)
                return false;
        }
        return true;
    }

    public static bool IsIgnoreCollisionSet() {
        return IsGenerated() &&
        Physics.GetIgnoreLayerCollision(LayerMask.NameToLayer("Hand"), LayerMask.NameToLayer("Hand")) &&
        Physics.GetIgnoreLayerCollision(LayerMask.NameToLayer("Hand"), LayerMask.NameToLayer("Grabbing")) &&

        Physics.GetIgnoreLayerCollision(LayerMask.NameToLayer("HandPlayer"), LayerMask.NameToLayer("Grabbable")) &&
        Physics.GetIgnoreLayerCollision(LayerMask.NameToLayer("HandPlayer"), LayerMask.NameToLayer("Grabbing")) &&
        Physics.GetIgnoreLayerCollision(LayerMask.NameToLayer("HandPlayer"), LayerMask.NameToLayer("Hand")) &&
        Physics.GetIgnoreLayerCollision(LayerMask.NameToLayer("HandPlayer"), LayerMask.NameToLayer("HandPlayer"));
    }



}
