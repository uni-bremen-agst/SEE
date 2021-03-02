using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace SEE.Game.UI.ConfigMenu
{
    public class PageController : DynamicUIBehaviour
    {
        public string headlineText;

        private TextMeshProUGUI _headline;

        public void Start()
        {
            MustGetComponentInChild("Heading", out _headline);
            _headline.text = headlineText;
        }
    }
    
}

