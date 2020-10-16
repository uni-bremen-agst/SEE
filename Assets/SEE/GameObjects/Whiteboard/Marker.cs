﻿/*
MIT License 
Copyright(c) 2017 MarekMarchlewicz

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using UnityEngine;

namespace SEE.GO.Whiteboard
{
    [System.Obsolete("Experimental code. Do not use it. May be removed soon.")]
    public class Marker : DraggableObject
    {
        [SerializeField]
        private Color color;

        [SerializeField]
        private readonly MeshRenderer[] colouredParts;

        [SerializeField]
        private readonly Painter painter; // FIXME: Never assigned.

        [SerializeField]
        private readonly PaintReceiver paintReceiver;

        protected override void Awake()
        {
            base.Awake();

            foreach (MeshRenderer renderer in colouredParts)
            {
                renderer.material.color = color;
            }

            painter.Initialize(paintReceiver);
            painter.ChangeColour(color);
        }
    }
}