using SEE.Game;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

namespace Assets.SEE.Game.Drawable
{
    [Serializable]
    public class Line :ICloneable
    {
        public GameObject gameObject;

        public string id;

        public Vector3 position;

        public Vector3 scale;

        public Vector3[] rendererPositions;

        public bool loop;

        public Color color;

        public int orderInLayer;

        public float thickness;

        public Vector3 holderEulerAngles;

        public Vector3 holderPosition;

        

        public static Line GetLine(GameObject lineGameObject)
        { 
            Line line = null;
            if (lineGameObject != null && lineGameObject.CompareTag(Tags.Line)) {
                Transform holderTransform = lineGameObject.transform.parent;
                line = new();
                line.gameObject = lineGameObject;
                line.id = lineGameObject.name;
                line.position = lineGameObject.transform.position;
                line.scale = lineGameObject.transform.localScale;
                LineRenderer renderer = lineGameObject.GetComponent<LineRenderer>();
                line.rendererPositions = new Vector3[renderer.positionCount];
                renderer.GetPositions(line.rendererPositions);
                line.loop = renderer.loop;
                line.holderEulerAngles = holderTransform.localEulerAngles;
                line.holderPosition = holderTransform.localPosition; 
                line.color = renderer.material.color;
                line.orderInLayer = renderer.sortingOrder;
                line.thickness = renderer.startWidth;
            }
            return  line;
        }

        public object Clone()
        {
            return new Line
            {
                gameObject = this.gameObject,
                id = this.id,
                position = this.position,
                rendererPositions = this.rendererPositions,
                loop = this.loop,
                color = this.color,
                orderInLayer = this.orderInLayer,
                thickness = this.thickness,
                holderEulerAngles = this.holderEulerAngles,
                holderPosition = this.holderPosition,
                scale = this.scale,
            };
        }
    }
}