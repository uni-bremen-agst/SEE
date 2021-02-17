using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Michsky.UI.ModernUIPack
{
    [ExecuteInEditMode]
    public class UIManagerDropdown : MonoBehaviour
    {
        [Header("SETTINGS")]
        public UIManager UIManagerAsset;

        [Header("RESOURCES")]
        public Image background;
        public Image contentBackground;
        public Image mainIcon;
        public TextMeshProUGUI mainText;
        public Image expandIcon;
        public Image itemBackground;
        public Image itemIcon;
        public TextMeshProUGUI itemText;
        CustomDropdown dropdownMain;
        DropdownMultiSelect dropdownMulti;

        void Awake()
        {
            try
            {
                if (dropdownMain != null)
                    dropdownMain = gameObject.GetComponent<CustomDropdown>();
                else
                    dropdownMulti = gameObject.GetComponent<DropdownMultiSelect>();

                if (UIManagerAsset == null)
                    UIManagerAsset = Resources.Load<UIManager>("MUIP Manager");

                this.enabled = true;

                if (UIManagerAsset.enableDynamicUpdate == false)
                {
                    UpdateDropdown();
                    this.enabled = false;
                }
            }

            catch
            {
                Debug.Log("<b>[Modern UI Pack]</b> No UI Manager found, assign it manually.", this);
            }
        }

        void LateUpdate()
        {
            if (UIManagerAsset == null)
                return;

            if (UIManagerAsset.enableDynamicUpdate == true)
                UpdateDropdown();
        }

        void UpdateDropdown()
        {
            try
            {
                if (UIManagerAsset.buttonThemeType == UIManager.ButtonThemeType.BASIC)
                {
                    background.color = UIManagerAsset.dropdownColor;
                    contentBackground.color = UIManagerAsset.dropdownColor;
                    mainIcon.color = UIManagerAsset.dropdownTextColor;
                    mainText.color = UIManagerAsset.dropdownTextColor;
                    expandIcon.color = UIManagerAsset.dropdownTextColor;
                    itemBackground.color = UIManagerAsset.dropdownItemColor;
                    itemIcon.color = UIManagerAsset.dropdownTextColor;
                    itemText.color = UIManagerAsset.dropdownTextColor;
                    mainText.font = UIManagerAsset.dropdownFont;
                    mainText.fontSize = UIManagerAsset.dropdownFontSize;
                    itemText.font = UIManagerAsset.dropdownFont;
                    itemText.fontSize = UIManagerAsset.dropdownFontSize;
                }

                else if (UIManagerAsset.buttonThemeType == UIManager.ButtonThemeType.CUSTOM)
                {
                    background.color = UIManagerAsset.dropdownColor;
                    contentBackground.color = UIManagerAsset.dropdownColor;
                    mainIcon.color = UIManagerAsset.dropdownIconColor;
                    mainText.color = UIManagerAsset.dropdownTextColor;
                    expandIcon.color = UIManagerAsset.dropdownIconColor;
                    itemBackground.color = UIManagerAsset.dropdownItemColor;
                    itemIcon.color = UIManagerAsset.dropdownItemIconColor;
                    itemText.color = UIManagerAsset.dropdownItemTextColor;
                    mainText.font = UIManagerAsset.dropdownFont;
                    mainText.fontSize = UIManagerAsset.dropdownFontSize;
                    itemText.font = UIManagerAsset.dropdownItemFont;
                    itemText.fontSize = UIManagerAsset.dropdownItemFontSize;
                }

                if (dropdownMain != null)
                {
                    if (UIManagerAsset.dropdownAnimationType == UIManager.DropdownAnimationType.FADING)
                        dropdownMain.animationType = CustomDropdown.AnimationType.FADING;

                    else if (UIManagerAsset.dropdownAnimationType == UIManager.DropdownAnimationType.SLIDING)
                        dropdownMain.animationType = CustomDropdown.AnimationType.SLIDING;

                    else if (UIManagerAsset.dropdownAnimationType == UIManager.DropdownAnimationType.STYLISH)
                        dropdownMain.animationType = CustomDropdown.AnimationType.STYLISH;
                }

                else if (dropdownMulti != null)
                {
                    if (UIManagerAsset.dropdownAnimationType == UIManager.DropdownAnimationType.FADING)
                        dropdownMulti.animationType = DropdownMultiSelect.AnimationType.FADING;

                    else if (UIManagerAsset.dropdownAnimationType == UIManager.DropdownAnimationType.SLIDING)
                        dropdownMulti.animationType = DropdownMultiSelect.AnimationType.SLIDING;

                    else if (UIManagerAsset.dropdownAnimationType == UIManager.DropdownAnimationType.STYLISH)
                        dropdownMulti.animationType = DropdownMultiSelect.AnimationType.STYLISH;
                }
            }

            catch { }
        }
    }
}