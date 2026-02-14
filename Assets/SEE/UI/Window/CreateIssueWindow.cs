
using SEE.GO;
using SEE.UI.Notification;
using SEE.UI.Window;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SEE.UI.Window
{
    public class CreateIssueWindow : BaseWindow
    {
        private readonly string ItemPrefab = UIPrefabFolder + "PropertyRowLineInput"; //"PropertyRowLine";
        private readonly string GroupPrefab = UIPrefabFolder + "PropertyGroupItem";
        /// <summary>
        /// Liste der Attribute, die bearbeitet werden können.
        /// Key = Anzeigename, Value = aktueller Wert
        /// </summary>
        private Dictionary<string, string> attributes = null;
        //new Dictionary<string, string>()
        //{
        //    { "Title", "" },
        //    { "Description", "" },
        //    { "Priority", "Medium" }
        //};
     
        private  Func<Dictionary<string, string>, Task<bool>> _func;
        private string nameFuncCall = "";


        public void Init(Func<Dictionary<string, string>, Task<bool>> callFunction, Dictionary<string, string> attributes_,string nameFuncCallp)
        {
            nameFuncCall = nameFuncCallp;
            attributes = attributes_;
            _func = callFunction;
        }


        // UI Content
        private RectTransform contentArea;

        protected override void StartDesktop()
        {
            base.StartDesktop();
            Controls.SEEInput.KeyboardShortcutsEnabled = false;
            // Im BaseWindow-Prefab muss existieren ein "Content"-Bereich
            contentArea = Window.transform.Find("Content").GetComponent<RectTransform>();
            if (Window.TryGetComponentOrLog(out RectTransform rect))
            {
    
                rect.sizeDelta = new(Screen.width, Screen.height);
            }   
            //RectTransform rt = this.GetComponent<RectTransform>();
            //float width = Screen.width * 0.6f;   // 50% Bildschirmbreite
            //float height = Screen.height * 0.5f;   // 30% Bildschirmhöhe
            // Resolution =
            //rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            //rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            //rt.anchorMin = new Vector2(0.1f, 0.2f);   // 10% von links, 20% von unten
            //rt.anchorMax = new Vector2(0.9f, 0.8f);   // 90% von rechts, 80% von oben

            //rt.offsetMin = Vector2.zero; // entspricht Left/Bottom = 0
            //rt.offsetMax = Vector2.zero; // entspricht Right/Top   = 0
            BuildUI();
        }

        /// <summary>
        /// Baut den kompletten Inhalt neu auf.
        /// </summary>
        private async Task BuildUI()
        {
            // Alte Elemente entfernen
            foreach (Transform child in contentArea)
                Destroy(child.gameObject);

            VerticalLayoutGroup verticalLayout = contentArea.GetComponent<VerticalLayoutGroup>();
            if (verticalLayout == null)
                verticalLayout = contentArea.gameObject.AddComponent<VerticalLayoutGroup>();

            verticalLayout.childAlignment = TextAnchor.UpperCenter;
            verticalLayout.spacing = 12f;
            verticalLayout.padding = new RectOffset(20, 20, 20, 20);

            // Attribute-Liste
            foreach (var pair in attributes)
            {
                GameObject rowObj = CreateAttributeRow(pair.Key, pair.Value);
                rowObj.transform.SetParent(contentArea, false);
                //TMP_InputField inputField = rowObj.GetComponentsInChildren<TMP_InputField>()
                //    .FirstOrDefault(f => f.gameObject.name == "InputField");
                //if (inputField != null)
                //{
                //    inputField.text = pair.Value;
                //}
            }
            // placeholderRow Row
            GameObject placeholderRow = new GameObject("placeholderRow", typeof(RectTransform));
            placeholderRow.transform.SetParent(contentArea, false);

            HorizontalLayoutGroup hLayout1 = placeholderRow.AddComponent<HorizontalLayoutGroup>();
            hLayout1.childAlignment = TextAnchor.MiddleCenter;


            // Bottom Button Row
            GameObject bottomRow = new GameObject("ButtonRow", typeof(RectTransform));
            bottomRow.transform.SetParent(contentArea, false);

            HorizontalLayoutGroup hLayout = bottomRow.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.spacing = 30f;

            // Buttons
            CreateButton(nameFuncCall, OnCallFunction, new Color(0.6f, 0.8f, 0.6f)).transform.SetParent(bottomRow.transform, false);
            CreateButton("Cancel", OnCancel, new Color(0.8f, 0.1f, 0.1f)).transform.SetParent(bottomRow.transform, false);
        }

        private GameObject CreateAttributeRow(string key, string value)
        {


          //  var placeholderRow = new GameObject("placeholderRow", typeof(RectTransform));
          //  placeholderRow.transform.SetParent(contentArea, false);

          //  var hLayout1 = placeholderRow.AddComponent<HorizontalLayoutGroup>();
          //  hLayout1.childAlignment = TextAnchor.MiddleCenter;


          //  // Bottom Button Row
          //  var bottomRow = new GameObject("ButtonRow", typeof(RectTransform));
          //  bottomRow.transform.SetParent(contentArea, false);

          //  HorizontalLayoutGroup hLayout = bottomRow.AddComponent<HorizontalLayoutGroup>();
          //  hLayout.childAlignment = TextAnchor.MiddleCenter;
          //  hLayout.spacing = 30f;

          //  GameObject btnGO = new GameObject("text", typeof(Button), typeof(Image));
          //  var btn = btnGO.GetComponent<Button>();

          //  // Hintergrundfarbe optional setzen
          //  var img = btnGO.GetComponent<Image>();
          //  img.color = Color.cyan;

          //  // Label
          //  GameObject labelGO = new GameObject("InputField", typeof(InputField));
          //  labelGO.transform.SetParent(btnGO.transform, false);

          //  var label5 = labelGO.GetComponent<TextMeshProUGUI>();
          //  label5.text = "text";
          //  label5.fontSize = 26;
          //  label5.alignment = TextAlignmentOptions.Center;

          //  btn.targetGraphic = img;
          ////  btn.onClick.AddListener(callback);

          //  // Größe
          //  RectTransform rt = btnGO.GetComponent<RectTransform>();
          //  rt.sizeDelta = new Vector2(200, 45);

          //  return btnGO;
            //bottomRow.transform.SetParent()

            GameObject row = PrefabInstantiator.InstantiatePrefab(ItemPrefab, contentArea, false);

            TextMeshProUGUI label = row.transform.GetComponentInChildren<TextMeshProUGUI>();
            label.text =$"{key}:";
            TMP_InputField input = row.GetComponentInChildren<TMP_InputField>();
           // ShowNotification.Error($"IssueWindow  {value}  name: {input.name}", "fs", 10);
            input.text = value;
            input.onValueChanged.AddListener(v => attributes[key] = v);
            //if (!input.text.Equals(value))
            //    input.text = value;

            return row;


            // ----- ROW -----
            //GameObject row = new GameObject(key + "_Row", typeof(RectTransform));
            //row.transform.SetParent(contentArea, false);

            //HorizontalLayoutGroup hLayout = row.AddComponent<HorizontalLayoutGroup>();
            //hLayout.childForceExpandWidth = false; // sehr wichtig!
            //hLayout.childControlWidth = true;      // wichtig, dass LayoutElement width benutzt
            //hLayout.spacing = 5f;

            //// ----- Label -----
            //GameObject labelGO = new GameObject("Label", typeof(TextMeshProUGUI), typeof(LayoutElement));
            //labelGO.transform.SetParent(row.transform, false);

            //TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
            //label.text = key;
            //label.fontSize = 26;
            //label.alignment = TextAlignmentOptions.Center;

            //// LayoutElement für 50%
            //LayoutElement labelLE = labelGO.GetComponent<LayoutElement>();
            //labelLE.minWidth = 0;
            //labelLE.preferredWidth = 0.5f * contentArea.GetComponent<RectTransform>().rect.width;
            //labelLE.flexibleWidth = 0;

            //// ----- InputField -----
            //GameObject inputGO = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
            //inputGO.transform.SetParent(row.transform, false);

            //// Hintergrundfarbe
            //inputGO.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);

            //TMP_InputField inputField = inputGO.GetComponent<TMP_InputField>();

            //// Text-Komponente für TMP_InputField
            //GameObject textGO = new GameObject("Text", typeof(TextMeshProUGUI));
            //textGO.transform.SetParent(inputGO.transform, false);
            //TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
            //text.alignment = TextAlignmentOptions.Center;
            //text.fontSize = 20;

            //RectTransform textRT = textGO.GetComponent<RectTransform>();
            //textRT.anchorMin = Vector2.zero;
            //textRT.anchorMax = Vector2.one;
            //textRT.offsetMin = new Vector2(5, 5);
            //textRT.offsetMax = new Vector2(-5, -5);

            //inputField.textComponent = text;
            //inputField.text = value;
            //inputField.lineType = TMP_InputField.LineType.MultiLineNewline;

            //// LayoutElement für 50%
            //LayoutElement inputLE = inputGO.GetComponent<LayoutElement>();
            //inputLE.minWidth = 0;
            //inputLE.preferredWidth = 0.5f * contentArea.GetComponent<RectTransform>().rect.width;
            //inputLE.flexibleWidth = 0;  // wichtig, sonst wächst es unkontrolliert
            // GameObject row = new GameObject(key + "_Row", typeof(RectTransform));
            // HorizontalLayoutGroup hLayout = row.AddComponent<HorizontalLayoutGroup>();
            // hLayout.childAlignment = TextAnchor.MiddleCenter;
            // hLayout.spacing = 5;
            // hLayout.childForceExpandWidth = false; // wichtig
            // hLayout.childControlWidth = true;

            // // ----- Label -----
            // GameObject labelGO = new GameObject("Label", typeof(TextMeshProUGUI), typeof(LayoutElement));
            // labelGO.transform.SetParent(row.transform, false);
            // var label = labelGO.GetComponent<TextMeshProUGUI>();
            // label.text = key;
            // label.fontSize = 26;
            // label.alignment = TextAlignmentOptions.Center;

            // // LayoutElement für 50%
            // var labelLE = labelGO.GetComponent<LayoutElement>();
            // labelLE.flexibleWidth = 1;
            // labelLE.preferredWidth = 0; // optional
            // labelLE.minWidth = 0;

            // // ----- InputField -----
            // GameObject inputGO = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
            // inputGO.transform.SetParent(row.transform, false);

            // // LayoutElement für 50%
            // var inputLE = inputGO.GetComponent<LayoutElement>();
            // inputLE.flexibleWidth = 1;
            // inputLE.preferredWidth = 0;
            // inputLE.minWidth = 0;

            // var bg = inputGO.GetComponent<Image>();
            // bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // // Text-Komponente
            // GameObject textGO = new GameObject(value, typeof(TextMeshProUGUI));
            // textGO.transform.SetParent(inputGO.transform, false);
            // var text = textGO.GetComponent<TextMeshProUGUI>();
            // text.fontSize = 20;
            // text.alignment = TextAlignmentOptions.Center;

            // // RectTransform für Text auf volle Fläche
            // RectTransform textRT = textGO.GetComponent<RectTransform>();
            // textRT.anchorMin = Vector2.zero;
            // textRT.anchorMax = Vector2.one;
            // textRT.offsetMin = new Vector2(5, 5);
            // textRT.offsetMax = new Vector2(-5, -5);

            // // TMP InputField einrichten
            // TMP_InputField inputField = inputGO.GetComponent<TMP_InputField>();
            //inputField.textComponent = text;


            // inputField.richText = true;
            // inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            // inputField.caretColor = Color.white;

            //inputField.onFocusSelectAll = false;
            //inputField.resetOnDeActivation = false;
            //// inputField.text = "";
            //// Cursor am Ende, nicht alles auswählen beim ersten Klick
            ////inputField.onSelect.AddListener(_ =>
            ////{
            ////    inputField.caretPosition = inputField.text.Length;
            ////});

            //

            // hLayout.transform.SetParent(row.transform, false);
            //// Hintergrundfarbe optional setzen
            //var img = btnGO.GetComponent<Image>();
            //img.color = new Color(0.8f, 0.8f, 0.8f);

            //// Label
            //GameObject labelGO = new GameObject("Label", typeof(TextMeshProUGUI));
            //labelGO.transform.SetParent(btnGO.transform, false);

            //var rowRT = row.GetComponent<RectTransform>();
            //rowRT.sizeDelta = new Vector2(400, 50); // Höhe und Breite des Containers (Breite wird von LayoutGroup übernommen)

            //HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            //layout.spacing = 10f;
            //layout.childAlignment = TextAnchor.MiddleCenter;
            //layout.childForceExpandWidth = true;   // wichtig für feste 50/50 Breite
            //layout.childControlWidth = false;       // wichtig für feste 50/50 Breite
            //layout.childControlHeight = true;

            //// ----- LABEL -----
            //GameObject labelGO = new GameObject("Label", typeof(TextMeshProUGUI), typeof(LayoutElement));
            //labelGO.transform.SetParent(row.transform, false);

            //var label = labelGO.GetComponent<TextMeshProUGUI>();
            //label.text = key + ":";
            //label.fontSize = 22;
            //label.alignment = TextAlignmentOptions.Center;

            //var labelLE = labelGO.GetComponent<LayoutElement>();
            //labelLE.flexibleWidth = 1;
            //labelLE.preferredWidth =0;
            //labelLE.minWidth = 0;

            //// ----- INPUTFIELD -----
            //GameObject inputGO = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            //inputGO.transform.SetParent(row.transform, false);

            //// LayoutElement für feste Breite 50%
            //var inputLE = inputGO.AddComponent<LayoutElement>();
            //inputLE.flexibleWidth = 1;
            //inputLE.preferredWidth = 0;
            //inputLE.minWidth = 0;

            //// Hintergrund
            //var bg = inputGO.GetComponent<Image>();
            //bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            //// Text-GameObject
            //GameObject textGO = new GameObject("Text", typeof(TextMeshProUGUI));
            //textGO.transform.SetParent(inputGO.transform, false);

            //var text = textGO.GetComponent<TextMeshProUGUI>();
            //text.text = value;
            //text.fontSize = 20;

            //text.overflowMode = TextOverflowModes.Overflow;
            //text.alignment = TextAlignmentOptions.Center;

            //// RectTransform für Text auf volle Fläche des InputField
            //var textRT = textGO.GetComponent<RectTransform>();
            //textRT.anchorMin = Vector2.zero;
            //textRT.anchorMax = Vector2.one;
            //textRT.offsetMin = new Vector2(5, 5);
            //textRT.offsetMax = new Vector2(-5, -5);

            //// TMP InputField Setup
            //var inputField = inputGO.GetComponent<TMP_InputField>();
            //inputField.textComponent = text;
            //inputField.text = value;
            //inputField.richText = true;
            //inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            //inputField.caretColor = Color.white;
            //inputField.onFocusSelectAll = false;
            //inputField.resetOnDeActivation = false;

            //// Cursor am Ende, nicht alles auswählen beim ersten Klick
            //inputField.onSelect.AddListener(_ =>
            //{
            //    inputField.caretPosition = inputField.text.Length;
            //});

            //inputField.onValueChanged.AddListener(v => attributes[key] = v);

            // Instanziere die Zeile als Prefab
            //GameObject testRow = PrefabInstantiator.InstantiatePrefab(
            //    GroupPrefab,
            //    contentArea.transform.Find("Content/Items"),
            //    false
            //);

            //            // Label setzen
            //            var label3 = testRow.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            //            if (label3 != null)
            //            {
            //                label3.text = "Test Row";
            //            }

            //            // InputField initialen Wert setzen
            //            var inputField5 = testRow.transform.Find("InputField")?.GetComponent<TMP_InputField>();
            //            if (inputField5 != null)
            //            {
            //                inputField5.text = "Hier dein Wert";
            //            }

            //  GameObject row = new GameObject(key + "_Row", typeof(RectTransform));
            //  HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            //  layout.spacing = 10f;
            //  layout.childAlignment = TextAnchor.MiddleCenter;
            //  layout.childForceExpandWidth = true;
            //  layout.childControlWidth = true;

            //  // -------------------------------------
            //  // LABEL (linke Hälfte)
            //  // -------------------------------------
            //  GameObject labelGO = new GameObject("Label", typeof(TextMeshProUGUI), typeof(LayoutElement));
            //  labelGO.transform.SetParent(row.transform, false);
            //  var label = labelGO.GetComponent<TextMeshProUGUI>();
            //  label.text = key + ":";
            //  label.fontSize = 22;
            //  label.alignment = TextAlignmentOptions.Center;

            //  var labelLE = labelGO.GetComponent<LayoutElement>();
            //  labelLE.flexibleWidth = 1;     // 50%
            //  labelLE.minWidth = 0;
            //  labelLE.preferredWidth = 0;

            //  // -------------------------------------
            //  // INPUTFIELD (rechte Hälfte)
            //  // -------------------------------------
            //  GameObject inputGO = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(LayoutElement));
            //  inputGO.transform.SetParent(row.transform, false);

            //  var inputLE = inputGO.GetComponent<LayoutElement>();
            //  inputLE.flexibleWidth = 1;     // 50%
            //  inputLE.minWidth = 0;

            //  inputLE.preferredWidth = 0;

            //  var bg = inputGO.GetComponent<Image>();
            //  bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            //  var inputRect = inputGO.GetComponent<RectTransform>();
            //  inputRect.sizeDelta = new Vector2(0, 32);

            //  // TextArea
            //  GameObject textArea = new GameObject("TextArea", typeof(RectTransform));
            //  textArea.transform.SetParent(inputGO.transform, false);

            //  var textAreaRT = textArea.GetComponent<RectTransform>();
            //  textAreaRT.anchorMin = Vector2.zero;
            //  textAreaRT.anchorMax = Vector2.one;
            //  textAreaRT.offsetMin = new Vector2(5, 5);
            //  textAreaRT.offsetMax = new Vector2(-5, -5);

            //  // Text
            //  GameObject textGO = new GameObject("Text", typeof(TextMeshProUGUI));
            //  textGO.transform.SetParent(textArea.transform, false);

            //  var text = textGO.GetComponent<TextMeshProUGUI>();
            //  text.fontSize = 20;
            //  text.alignment = TextAlignmentOptions.Midline;
            //  text.raycastTarget = false;

            //  var textRT = textGO.GetComponent<RectTransform>();
            //  textRT.anchorMin = Vector2.zero;
            //  textRT.anchorMax = Vector2.one;
            //  textRT.offsetMin = Vector2.zero;
            //  textRT.offsetMax = Vector2.zero;

            //  //// Placeholder
            //  //GameObject placeholderGO = new GameObject("", typeof(TextMeshProUGUI));
            //  //placeholderGO.transform.SetParent(textArea.transform, false);

            //  //var placeholder = placeholderGO.GetComponent<TextMeshProUGUI>();
            //  //placeholder.text = "Enter " + key;
            //  //placeholder.fontSize = 20;
            //  //placeholder.color = new Color(1, 1, 1, 0.3f);
            //  //placeholder.alignment = TextAlignmentOptions.Center;
            //  //placeholder.raycastTarget = false;

            //  //var placeholderRT = placeholderGO.GetComponent<RectTransform>();
            //  //placeholderRT.anchorMin = Vector2.zero;
            //  //placeholderRT.anchorMax = Vector2.one;
            //  //placeholderRT.offsetMin = Vector2.zero;
            //  //placeholderRT.offsetMax = Vector2.zero;

            //  // TMP InputField Setup
            //  var inputField = inputGO.AddComponent<TMP_InputField>();
            //  //text.wordWrappingRatios = true;
            ////  text.overflowMode = TextOverflowModes.Page;
            //  inputField.textViewport = textAreaRT;
            //  inputField.textComponent = text;

            //  inputField.richText = true;
            //  inputField.lineType = TMP_InputField.LineType.MultiLineSubmit;
            //  inputField.text = value;

            //  inputField.caretColor = Color.white;
            //  inputField.selectionColor = new Color(0, 0, 0, 0);
            //  inputField.onFocusSelectAll = false;

            // Fix: Kein SelectAll beim ersten Klick

            //   inputField.onValueChanged.AddListener(v => attributes[key] = v);

            return row;
        }

        private GameObject CreateButton(string text, UnityEngine.Events.UnityAction callback, Color color)
        {
            GameObject btnGO = new GameObject(text, typeof(Button), typeof(Image));
            var btn = btnGO.GetComponent<Button>();

            // Hintergrundfarbe optional setzen
            var img = btnGO.GetComponent<Image>();
            img.color = color;

            // Label
            GameObject labelGO = new GameObject("Label", typeof(TextMeshProUGUI));
            labelGO.transform.SetParent(btnGO.transform, false);

            var label = labelGO.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = 26;
            label.alignment = TextAlignmentOptions.Center;

            btn.targetGraphic = img;
            btn.onClick.AddListener(callback);

            // Größe
            RectTransform rt = btnGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 45);

            return btnGO;
        }
        // Gibt immer alle Rows zurück so kann in jedem IssueProvider die richtigen Attribute gesetzt werden
        //public Dictionary<string, string> getAttributes()
        //{
        //    return new Dictionary<string, string> { };
        //}
        private void OnCallFunction()
        {
            Debug.Log("Issue erstellt mit Daten:");
            foreach (var p in attributes)
                Debug.Log($"- {p.Key}: {p.Value}");
             _func(attributes);
            Controls.SEEInput.KeyboardShortcutsEnabled = true;
        
            Window.SetActive(false);
         
           
         //   Destroy(this.Window);
        }
        private void OnCancel()
        {
          
            Window.SetActive(false);
            Controls.SEEInput.KeyboardShortcutsEnabled = true;
            // Destroy(this.Window);
        }

        public override void RebuildLayout()
        {
            BuildUI();
        }

        protected override void InitializeFromValueObject(WindowValues valueObject)
        {
            throw new NotImplementedException();
            // Falls später Attribute über Netzwerk gesetzt werden
        }

        public override void UpdateFromNetworkValueObject(WindowValues valueObject)
        {
            // Falls später Netzwerk-Updates kommen
            throw new NotImplementedException();
        }

        public override WindowValues ToValueObject()
        {
            return new WindowValues(Title);
        }
    }
}