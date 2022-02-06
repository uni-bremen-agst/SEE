using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UMA;
using UMA.CharacterSystem;

namespace NesalisGames
{
    namespace ClothingPack
    {
        public class CharacterInitializer : MonoBehaviour
        {

            public float Delay = 0.1f;

            private DynamicCharacterAvatar.ColorValueList defaultColors;
            private DynamicCharacterAvatar avatar;

            private void Awake()
            {
                avatar = GetComponent<DynamicCharacterAvatar>();

                //Setting default character colors
                defaultColors = new DynamicCharacterAvatar.ColorValueList();
                var colors = avatar.ActiveColors;

                foreach (var color in colors)
                {
                    defaultColors.SetColor(color.name, color.Color);
                }
            }

            // Delayed invocation of initialization
            public void OnCharacterCreated(UMAData data)
            {
                Invoke("Initialize", Delay);
            }

            // Character colors and wardrobes initialization.
            public void Initialize()
            {
                avatar.ClearSlots();
                avatar.LoadDefaultWardrobe();
                avatar.BuildCharacter(true);

                avatar.characterColors = defaultColors;
                avatar.BuildCharacter(true);
            }
        }
    }
}
