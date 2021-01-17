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


        public float MarkerX { get => MarkerX; set => MarkerX = value; }
        public float MarkerY { get => MarkerY; set => MarkerY = value; }
        public float MarkerZ { get => MarkerZ; set => MarkerZ = value; }
        public float CommentX { get => CommentX; set => CommentX = value; }
        public float CommentY { get => CommentY; set => CommentY = value; }
        public float CommentZ { get => CommentZ; set => CommentZ = value; }
        public string Comment { get => Comment; set => Comment = value; }
    }
}