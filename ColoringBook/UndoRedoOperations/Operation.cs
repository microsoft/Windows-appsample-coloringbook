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
using ColoringBook.Components;
using Windows.UI;
using Windows.UI.Input.Inking;

namespace ColoringBook.UndoRedoOperations
{
    internal abstract class Operation
    {
        protected Operation(uint transactionId) => TransactionId = transactionId;

        public uint TransactionId { get; }
    }

    public enum UndoRedoOperation
    {
        None,
        AddStrokes,
        EraseStrokes,
        EraseAllStrokes,
        FillCell,
        EraseCell,
        EraseAllCells
    }

    public interface IUndoRedoOperation
    {
        void Undo();

        void Redo();

        UndoRedoOperation GetUndoRedoOperation();

        uint TransactionId { get; }
    }

    public struct StrokeOperationArgs
    {
        public InkStrokeContainer StrokeContainer { get; set; }

        public IReadOnlyList<InkStroke> ModifiedStrokes { get; set; }
    }

    public struct CellOperationArgs
    {
        public InkCellController CellController { get; set; }

        public uint CellId { get; set; }

        public Color NewColor { get; set; }

        public Color PreviousColor { get; set; }

        public Dictionary<uint, Color> ColorDictionary { get; set; }
    }
}