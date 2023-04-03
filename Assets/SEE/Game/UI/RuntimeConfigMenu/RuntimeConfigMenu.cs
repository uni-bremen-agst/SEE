using System;
using System.Collections.Generic;
using System.Linq;
using SEE.Controls;
using SEE.DataModel;
using SEE.Game.City;
using UnityEngine;

namespace SEE.Game.UI.RuntimeConfigMenu
{
    public class RuntimeConfigMenu : MonoBehaviour
    {
        private static RuntimeTabMenu[] CityMenus;
        private int currentCity;
        
        void Start()
        {
            int cityCount = GameObject.FindGameObjectsWithTag(Tags.CodeCity).Length;
            CityMenus = new RuntimeTabMenu[cityCount];
            for (int i = 0; i < cityCount; i++)
            {
                CityMenus[i] = gameObject.AddComponent<RuntimeTabMenu>();
                CityMenus[i].Title = "City Configuration";
                CityMenus[i].HideAfterSelection = false;
                CityMenus[i].CityIndex = i;
                CityMenus[i].OnSwitchCity += SwitchCity;
            }
        }
        
        void Update()
        {
            if (SEEInput.ToggleConfigMenu()) CityMenus[currentCity].ToggleMenu();
        }

        private void SwitchCity(int i)
        {
            if (i == currentCity) return;
            CityMenus[currentCity].ShowMenu = false;
            CityMenus[i].ShowMenu = true;
            currentCity = i;
        }

        public static AbstractSEECity[] GetCities()
        {
            return GameObject.FindGameObjectsWithTag(Tags.CodeCity).Select(go => go.GetComponent<AbstractSEECity>())
                .OrderBy(go => go.name).ToArray();
        }
        
        public static AbstractSEECity GetCity(int i) => GetCities()[i];
        public static RuntimeTabMenu GetMenuForCity(int i) => CityMenus[i];
    }
}
