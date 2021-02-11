using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// Container for SliderMarker
    /// </summary>
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
        /// Returns the SliderMarker that is (approximately) at the given location or null if there is none
        /// </summary>
        /// <param name="location"> Location to search for SliderMarker at </param>
        /// <returns> SliderMarker that is (approximately) at the given location or null if there is none </returns>
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
        /// <param name="path"> Path to save at. </param>
        public void Save(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(SliderMarkerContainer));
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
                serializer.Serialize(stream, this);
            }
        }

        /// <summary>
        /// Loads a SliderMarkerContainer from a path
        /// </summary>
        /// <param name="path"> Path to load from. </param>
        public static SliderMarkerContainer Load(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(SliderMarkerContainer));
            using (FileStream stream = new FileStream(path, FileMode.Open))
            {
                return serializer.Deserialize(stream) as SliderMarkerContainer;
            }
        }

        /// <summary>
        /// Loads a SliderMarkerContainer from text
        /// </summary>
        /// <param name="text"> Text to load from. </param>
        public static SliderMarkerContainer LoadFromText(string text)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(SliderMarkerContainer));
            return serializer.Deserialize(new StringReader(text)) as SliderMarkerContainer;
        }
    }
}
