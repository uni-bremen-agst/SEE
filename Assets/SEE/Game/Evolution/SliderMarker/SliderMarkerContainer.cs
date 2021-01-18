using System.Collections.Generic;
using UnityEngine;
using System.Xml.Serialization;
using System.IO;

namespace SEE.Game.Evolution
{
    [XmlRoot("SliderMarkerCollection")]
    public class SliderMarkerContainer
    {

        /// <summary>
        /// List of slider markers
        /// </summary>
        [XmlArray("SliderMarkers")]
        [XmlArrayItem("SliderMarker")]
        public List<SliderMarker> SliderMarkers = new List<SliderMarker>();

        /// <summary>
        /// Returns the slider marker that is (approximately) at a given location or null if there is none
        /// </summary>
        public SliderMarker getSliderMarkerForLocation(Vector3 location)
        {
            foreach (SliderMarker sliderMarker in SliderMarkers)
            {
                if (Mathf.Approximately(sliderMarker.MarkerX, location.x) && Mathf.Approximately(sliderMarker.MarkerY, location.y) && Mathf.Approximately(sliderMarker.MarkerZ, location.z))
                {
                    return sliderMarker;
                }
            }
            return null;
        }

        /// <summary>
        /// Saves the slider markers
        /// </summary>
        public void Save(string path)
        {
            var serializer = new XmlSerializer(typeof(SliderMarkerContainer));
            using (var stream = new FileStream(path, FileMode.Create))
            {
                serializer.Serialize(stream, this);
            }
        }

        /// <summary>
        /// Loads a SliderMarkerContainer from a path
        /// </summary>
        public static SliderMarkerContainer Load(string path)
        {
            var serializer = new XmlSerializer(typeof(SliderMarkerContainer));
            using (var stream = new FileStream(path, FileMode.Open))
            {
                return serializer.Deserialize(stream) as SliderMarkerContainer;
            }
        }

        /// <summary>
        /// Loads a SliderMarkerContainer from text
        /// </summary>
        public static SliderMarkerContainer LoadFromText(string text)
        {
            var serializer = new XmlSerializer(typeof(SliderMarkerContainer));
            return serializer.Deserialize(new StringReader(text)) as SliderMarkerContainer;
        }
    }
}

