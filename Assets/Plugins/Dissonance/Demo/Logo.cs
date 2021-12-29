using UnityEngine;

namespace Dissonance.Demo
{
    public class Logo : MonoBehaviour
    {
        private Texture2D _logo;

        public void Awake()
        {
            _logo = Resources.Load<Texture2D>("dissonance_logo");
        }

        private void OnGUI()
        {
            GUI.DrawTexture(new Rect(Screen.width - _logo.width - 10, 10, _logo.width, _logo.height), _logo);
        }
    }
}