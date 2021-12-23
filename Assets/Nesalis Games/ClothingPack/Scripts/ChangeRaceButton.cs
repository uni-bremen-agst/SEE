using System.Collections;
using System.Collections.Generic;
using UMA.CharacterSystem;
using UnityEngine;
using UnityEngine.UI;

namespace NesalisGames
{
    namespace ClothingPack
    {
        public class ChangeRaceButton : MonoBehaviour
        {
            public DynamicCharacterAvatar Avatar;
            public Text ActiveRace;
            public List<UMA.RaceData> Races = new List<UMA.RaceData>();

            private int index = 1;

            public void ChangeRaceClick()
            {
                Avatar.ChangeRace(Races[index]);
                ActiveRace.text = "Active race: " + Races[index].raceName;

                index++;
                if (index > Races.Count - 1) index = 0;
            }
        }
    }
}
