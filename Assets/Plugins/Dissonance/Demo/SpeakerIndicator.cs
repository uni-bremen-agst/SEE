using System.Collections;
using JetBrains.Annotations;
using UnityEngine;

namespace Dissonance.Demo
{
    public class SpeakerIndicator
        : MonoBehaviour
    {
        private GameObject _indicator;
        private Light _light;
        private Transform _transform;

        private float _intensity;

        private IDissonancePlayer _player;
        private VoicePlayerState _state;

        private bool IsSpeaking
        {
            get { return _player != null && _player.Type == NetworkPlayerType.Remote && _state != null && _state.IsSpeaking; }
        }

        private void OnEnable()
        {
            //Get some bits from the indicator game object
            _indicator = Instantiate(Resources.Load<GameObject>("SpeechIndicator"));
            _indicator.transform.SetParent(transform);
            _indicator.transform.localPosition = new Vector3(0, 3, 0);

            _light = _indicator.GetComponent<Light>();
            _transform = _indicator.GetComponent<Transform>();

            //Find the component attached to this game object which marks it as a Dissonance player representation
            _player = GetComponent<IDissonancePlayer>();

            StartCoroutine(FindPlayerState());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private IEnumerator FindPlayerState()
        {
            // Wait until player script is attached
            while (_player == null)
            {
                _player = GetComponent<IDissonancePlayer>();
                yield return null;
            }

            // Wait until player tracking has initialized
            while (!_player.IsTracking)
                yield return null;

            // Now ask Dissonance for the object which represents the state of this player
            // The loop is necessary in case Dissonance is still initializing this player into the network session
            while (_state == null)
            {
                _state = FindObjectOfType<DissonanceComms>().FindPlayer(_player.PlayerId);
                yield return null;
            }
        }

        private void Update()
        {
            if (IsSpeaking)
            {
                //Calculate intensity of speech - do the pow to visually boost the scale at lower intensities
                _intensity = Mathf.Max(Mathf.Clamp(Mathf.Pow(_state.Amplitude, 0.175f), 0.25f, 1), _intensity - Time.unscaledDeltaTime);
                _indicator.SetActive(true);
            }
            else
            {
                //Fade out intensity when player is not talking
                _intensity -= Time.unscaledDeltaTime * 2;

                if (_intensity <= 0)
                    _indicator.SetActive(false);
            }

            UpdateLight(_light, _intensity);
            UpdateChildTransform(_transform, _intensity);
        }

        private static void UpdateChildTransform([NotNull] Transform transform, float intensity)
        {
            transform.localScale = new Vector3(intensity, intensity, intensity);
        }

        private static void UpdateLight([NotNull] Light light, float intensity)
        {
            light.intensity = intensity;
        }
    }
}
