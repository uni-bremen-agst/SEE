
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

        private  Func<Dictionary<string, string>, Task<bool>> func;
        private string nameFuncCall = "";


        public void Init(Func<Dictionary<string, string>, Task<bool>> callFunction, Dictionary<string, string> attributes_,string nameFuncCallp)
        {
            nameFuncCall = nameFuncCallp;
            attributes = attributes_;
            func = callFunction;
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
                rect.sizeDelta = new(Screen.width*0.7f, Screen.height*0.7f);
            }   
            BuildUI();
        }

        /// <summary>
        /// Create the Content.
        /// 1. Rows with Attributes
        /// 2. Bottom Row with Functional Button + Chancel
        /// </summary>
        private async Task BuildUI()
        {
            //// Alte Elemente entfernen
            //foreach (Transform child in contentArea)
            //    Destroy(child.gameObject);

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

            GameObject row = PrefabInstantiator.InstantiatePrefab(ItemPrefab, contentArea, false);

            TextMeshProUGUI label = row.transform.GetComponentInChildren<TextMeshProUGUI>();
            label.text =$"{key}:";
            TMP_InputField input = row.GetComponentInChildren<TMP_InputField>();

            input.text = value;
            input.onValueChanged.AddListener(v => attributes[key] = v);
  
            return row;
        }

        private GameObject CreateButton(string text, UnityEngine.Events.UnityAction callback, Color color)
        {
            GameObject btnGO = new GameObject(text, typeof(Button), typeof(Image));
            var btn = btnGO.GetComponent<Button>();

            // set color for the Button
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

            // Size 
            RectTransform rt = btnGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 45);

            return btnGO;
        }

        private void OnCallFunction()
        {
            Debug.Log("Issue erstellt mit Daten:");
            foreach (var p in attributes)
                Debug.Log($"- {p.Key}: {p.Value}");
            func(attributes);
            Controls.SEEInput.KeyboardShortcutsEnabled = true;
      
            Window.SetActive(false);
         
           
        }
        private void OnCancel()
        {
            Window.SetActive(false);
            Controls.SEEInput.KeyboardShortcutsEnabled = true;
        }

        public override void RebuildLayout()
        {
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