using System.Xml.Serialization;

namespace SEE.Game.Evolution
{
    /// <summary>
    /// Class for storing the location and comments of markers.
    /// </summary>
    public class SliderMarker
    {
        /// <summary>
        /// x coordinate of the slider marker
        /// </summary>
        [XmlAttribute("MarkerX")]
        public float MarkerX;

        /// <summary>
        /// y coordinate of the slider marker
        /// </summary>
        [XmlAttribute("MarkerY")]
        public float MarkerY;

        /// <summary>
        /// z coordinate of the slider marker
        /// </summary>
        [XmlAttribute("MarkerZ")]
        public float MarkerZ;

        /// <summary>
        /// comment of the slider marker
        /// </summary>
        [XmlAttribute("Comment")]
        public string Comment;

        /// <summary>
        /// Set the comment of the slider marker
        /// </summary>
        /// <param name="comment"> Comment to set </param>
        public void SetComment(string comment)
        {
            this.Comment = comment;
        }
    }
}