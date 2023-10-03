using SEE.Game;
using System;
using System.Collections;
using System.Linq.Expressions;
using UnityEngine;

namespace Assets.SEE.Game.Drawable
{
    public class LineKindHolder : MonoBehaviour, ICloneable
    {
        private GameDrawer.LineKind lineKind;

        public void SetLineKind(GameDrawer.LineKind lineKind)
        {
            this.lineKind = lineKind;
        }

        public GameDrawer.LineKind GetLineKind()
        {
            return lineKind;
        }

        public object Clone()
        {
            return new LineKindHolder
            {
                lineKind = this.lineKind
            };
        }
    }
}