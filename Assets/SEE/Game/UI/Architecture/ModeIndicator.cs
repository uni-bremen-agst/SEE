using System;
using TMPro;
using UnityEngine;

namespace SEE.Game.UI.Architecture
{
    /// <summary>
    /// Component that updates the attached <see cref="TextMeshProUGUI"/> according to the selected AbstractArchitectureAction
    /// </summary>
    public class ModeIndicator : MonoBehaviour
    {
        private TextMeshProUGUI Text;
        
        private void Awake()
        {
            Text = GetComponentInChildren<TextMeshProUGUI>();
        }


        /// <summary>
        /// Changes the text of the <see cref="TextMeshProUGUI"/>.
        /// </summary>
        /// <param name="text"></param>
        public void ChangeStateIndicator(String text)
        {
            Text.text = text;
        }
    }
}