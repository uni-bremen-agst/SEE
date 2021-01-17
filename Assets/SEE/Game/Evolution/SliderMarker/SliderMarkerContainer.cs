using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace SEE.Game.Evolution
{
    [XmlRoot("SliderMarkerCollection")]
    public class SliderMarkerContainer
    {

        [XmlArray("SliderMarkers")]
        [XmlArrayItem("SliderMarker")]
        public SliderMarker[] SliderMarkers;

        public void Save(string path)
        {
            var serializer = new XmlSerializer(typeof(SliderMarkerContainer));
            using (var stream = new FileStream(path, FileMode.Create))
            {
                serializer.Serialize(stream, this);
            }
        }

        public static SliderMarkerContainer Load(string path)
        {
            var serializer = new XmlSerializer(typeof(SliderMarkerContainer));
            using (var stream = new FileStream(path, FileMode.Open))
            {
                return serializer.Deserialize(stream) as SliderMarkerContainer;
            }
        }

        public static SliderMarkerContainer LoadFromText(string text)
        {
            var serializer = new XmlSerializer(typeof(SliderMarkerContainer));
            return serializer.Deserialize(new StringReader(text)) as SliderMarkerContainer;
        }
    }
}

