// ---------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//
// The MIT License (MIT)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// ---------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using ColoringBook.Common;
using Windows.UI.Input.Inking;

namespace ColoringBook.UndoRedoOperations
{
    internal class UndoRedoStrokeManager
    {
        private Dictionary<uint, InkStroke> StrokeMap { get; } = new Dictionary<uint, InkStroke>();

        private Dictionary<uint, uint> RefCountMap { get; } = new Dictionary<uint, uint>();

        private uint TotalStrokeCount { get; set; }

        public InkStroke GetStroke(uint id)
        {
            try
            {
                StrokeMap.TryGetValue(id, out InkStroke stroke);
                return stroke;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public uint AddStroke(InkStroke inkStroke)
        {
            if (StrokeMap.ContainsValue(inkStroke))
            {
                return StrokeMap.FirstOrDefault(x => x.Value == inkStroke).Key;
            }

            // Add new stroke.
            var newId = TotalStrokeCount;
            ++TotalStrokeCount;
            StrokeMap.Add(newId, inkStroke);
            RefCountMap.Add(newId, 0);
            return newId;
        }

        public IReadOnlyList<uint> AddStrokes(IReadOnlyList<InkStroke> inkStrokes)
        {
            var strokeIds = new List<uint>();
            var strokeIdArray = new uint[inkStrokes.Count];

            if (inkStrokes.Count == 0)
            {
                return strokeIds.AsReadOnly();
            }

            // Duplicate list.
            var strokesToAdd = inkStrokes.ToList();

            // Keep a second copy that we can search through.
            var inkStrokesSearchList = inkStrokes.ToList();

            // First search through existing strokes.
            foreach (var pair in StrokeMap)
            {
                var index = strokesToAdd.FindIndex(stroke => stroke == pair.Value);
                if (index >= 0)
                {
                    strokesToAdd.RemoveAt(index);

                    var index2 = inkStrokesSearchList.FindIndex(stroke => stroke == pair.Value);
                    if (index2 >= 0)
                    {
                        strokeIdArray[index2] = pair.Key;
                    }
                }
            }

            // Add remaining strokes.
            foreach (var stroke in strokesToAdd)
            {
                uint newId = TotalStrokeCount;
                ++TotalStrokeCount;
                StrokeMap.Add(newId, stroke);
                RefCountMap.Add(newId, 0);

                var index = inkStrokesSearchList.FindIndex(listStroke => listStroke == stroke);
                if (index >= 0)
                {
                    strokeIdArray[index] = newId;
                }
            }

            // Ensure stroke order is identical.
            for (uint i = 0; i < inkStrokes.Count; i++)
            {
                strokeIds.Add(strokeIdArray[i]);
            }

            return strokeIds.AsReadOnly();
        }

        public void UpdateStroke(uint id, InkStroke newInkStroke)
        {
            if (StrokeMap.ContainsKey(id))
            {
                StrokeMap[id] = newInkStroke;
            }
        }

        public InkStroke CloneAndUpdateStroke(uint id)
        {
            var strokeClone = Tools.CloneInkStroke(GetStroke(id));
            UpdateStroke(id, strokeClone);
            return strokeClone;
        }

        public void Clear()
        {
            StrokeMap.Clear();
            RefCountMap.Clear();
            TotalStrokeCount = 0;
        }

        public void AddRef(uint strokeId)
        {
            if (RefCountMap.ContainsKey(strokeId))
            {
                RefCountMap[strokeId]++;
            }
        }

        public void RemoveRef(uint strokeId)
        {
            if (RefCountMap.ContainsKey(strokeId))
            {
                var currentRef = RefCountMap[strokeId];

                if (currentRef <= 1)
                {
                    RefCountMap.Remove(strokeId);
                    StrokeMap.Remove(strokeId);
                }
                else
                {
                    RefCountMap[strokeId]--;
                }
            }
        }
    }
}
