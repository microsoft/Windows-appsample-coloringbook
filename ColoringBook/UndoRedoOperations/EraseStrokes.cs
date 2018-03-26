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

using System.Collections.Generic;
using Windows.UI.Input.Inking;

namespace ColoringBook.UndoRedoOperations
{
    internal class EraseStrokes : Operation, IUndoRedoOperation
    {
        public EraseStrokes(uint transactionId, InkStrokeContainer inkStrokeContainer,
            UndoRedoStrokeManager strokeManager, IReadOnlyList<uint> strokeIds)
            : base(transactionId)
        {
            InkStrokeContainer = inkStrokeContainer;
            StrokeManager = strokeManager;
            StrokeIds = strokeIds;

            foreach (var strokeId in StrokeIds)
            {
                StrokeManager.AddRef(strokeId);
            }
        }

        ~EraseStrokes()
        {
            foreach (var strokeId in StrokeIds)
            {
                StrokeManager.RemoveRef(strokeId);
            }
        }

        private InkStrokeContainer InkStrokeContainer { get; }

        private UndoRedoStrokeManager StrokeManager { get; }

        private IReadOnlyList<uint> StrokeIds { get; }

        public void Undo()
        {
            foreach (var strokeId in StrokeIds)
            {
                var stroke = StrokeManager.CloneAndUpdateStroke(strokeId);
                InkStrokeContainer.AddStroke(stroke);
            }
        }

        public void Redo()
        {
            foreach (var strokeId in StrokeIds)
            {
                var stroke = StrokeManager.GetStroke(strokeId);
                stroke.Selected = true;
            }

            InkStrokeContainer.DeleteSelected();
        }

        public UndoRedoOperation GetUndoRedoOperation() => UndoRedoOperation.EraseStrokes;
    }
}
