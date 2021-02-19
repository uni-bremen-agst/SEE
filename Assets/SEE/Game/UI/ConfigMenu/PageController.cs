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
        private SEECity city;

        public void Start()
        {
            var impl = GameObject.Find("Implementation");
            city = impl.GetComponent<SEECity>();
            
            MustGetComponentInChild("Heading", out _headline);
            _headline.text = headlineText;
        }

        List<string> EnumToStr<EnumType>() where EnumType : Enum
        {
            return Enum.GetValues(typeof(EnumType)).Cast<EnumType>().Select(v => v.ToString())
                .ToList();
        }
    }
    
}

