using System.Xml;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SEE.Utils;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SEE.GO;

namespace SEE.Game.Evolution
{
    public class SliderMarker
    {
        [XmlAttribute("MarkerX")]
        public float MarkerX;
        [XmlAttribute("MarkerY")]
        public float MarkerY;
        [XmlAttribute("MarkerZ")]
        public float MarkerZ;
        [XmlAttribute("Comment")]
        public string Comment;

        public void setComment(string comment)
        {
            this.Comment = comment;
        }
    }
}