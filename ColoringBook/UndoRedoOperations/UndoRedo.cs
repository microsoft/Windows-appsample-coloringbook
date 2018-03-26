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
using ColoringBook.Common;

namespace ColoringBook.UndoRedoOperations
{
    public class UndoRedo
    {
        private uint TransactionId { get; set; }

        private uint HighestUsedTransactionId { get; set; }

        private uint ActiveTransactionIdCount { get; set; }

        private LinkedList<IUndoRedoOperation> RedoStack { get; } = new LinkedList<IUndoRedoOperation>();

        private LinkedList<IUndoRedoOperation> UndoStack { get; } = new LinkedList<IUndoRedoOperation>();

        private UndoRedoStrokeManager StrokeManager { get; } = new UndoRedoStrokeManager();

        // Used for readability.
        public void StartTransaction()
        {
        }

        public void EndTransaction()
        {
            if (TransactionId <= HighestUsedTransactionId)
            {
                TransactionId++;
                ActiveTransactionIdCount++;
            }
        }

        public void Reset()
        {
            UndoStack.Clear();
            RedoStack.Clear();
            StrokeManager.Clear();
            TransactionId = 0;
            HighestUsedTransactionId = 0;
            ActiveTransactionIdCount = 0;
        }

        private void AddOperationToUndoStack(IUndoRedoOperation operation)
        {
            if (ActiveTransactionIdCount >= Constants.UndoRedoTransactionCapacity)
            {
                var transactionIdToRemove = UndoStack.First?.Value.TransactionId;
                while (UndoStack.First?.Value.TransactionId == transactionIdToRemove)
                {
                    UndoStack.RemoveFirst();
                }

                --ActiveTransactionIdCount;
            }

            UndoStack.AddLast(operation);
        }

        private void ClearRedoStack()
        {
            if (RedoStack.Count != 0)
            {
                RedoStack.Clear();
            }
        }

        public bool HasUndoOperations() => UndoStack.Count != 0;

        public bool HasRedoOperations() => RedoStack.Count != 0;

        public void InsertUndoRedoOperation(UndoRedoOperation operation, object operationArgs)
        {
            switch (operation)
            {
                case UndoRedoOperation.AddStrokes:
                    InsertAddStrokeOperation((StrokeOperationArgs)operationArgs);
                    break;
                case UndoRedoOperation.EraseStrokes:
                    InsertEraseStrokeOperation((StrokeOperationArgs)operationArgs);
                    break;
                case UndoRedoOperation.EraseAllStrokes:
                    InsertEraseAllStrokesOperation((StrokeOperationArgs)operationArgs);
                    break;
                case UndoRedoOperation.FillCell:
                    InsertFillCellOperation((CellOperationArgs)operationArgs);
                    break;
                case UndoRedoOperation.EraseCell:
                    InsertEraseCellOperation((CellOperationArgs)operationArgs);
                    break;
                case UndoRedoOperation.EraseAllCells:
                    InsertEraseAllCellsOperation((CellOperationArgs)operationArgs);
                    break;
                case UndoRedoOperation.None:
                default:
                    break;
            }

            if (operation != UndoRedoOperation.None)
            {
                HighestUsedTransactionId = TransactionId;
                ClearRedoStack();
            }
        }

        private void InsertAddStrokeOperation(StrokeOperationArgs args) => AddOperationToUndoStack(
            new AddStrokes(TransactionId, args.StrokeContainer, StrokeManager,
                StrokeManager.AddStrokes(args.ModifiedStrokes)));

        private void InsertEraseStrokeOperation(StrokeOperationArgs args) => AddOperationToUndoStack(
            new EraseStrokes(TransactionId, args.StrokeContainer, StrokeManager,
                StrokeManager.AddStrokes(args.ModifiedStrokes)));

        private void InsertEraseAllStrokesOperation(StrokeOperationArgs args) => AddOperationToUndoStack(
            new EraseAllStrokes(TransactionId, args.StrokeContainer, StrokeManager,
                StrokeManager.AddStrokes(args.ModifiedStrokes)));

        private void InsertFillCellOperation(CellOperationArgs args) => AddOperationToUndoStack(
            new FillCell(TransactionId, args.CellController, args.CellId, args.NewColor, args.PreviousColor));

        private void InsertEraseCellOperation(CellOperationArgs args) => AddOperationToUndoStack(
            new EraseCell(TransactionId, args.CellController, args.CellId, args.PreviousColor));

        private void InsertEraseAllCellsOperation(CellOperationArgs args) => AddOperationToUndoStack(
            new EraseAllCells(TransactionId, args.CellController, args.ColorDictionary));

        public void Undo()
        {
            if (UndoStack.Count == 0)
            {
                return;
            }

            var operationNode = UndoStack.Last;
            var currentTransactionId = operationNode.Value.TransactionId;
            while (operationNode != null && operationNode.Value.TransactionId == currentTransactionId)
            {
                var prev = operationNode.Previous;
                var operation = operationNode.Value;
                operation.Undo();

                // Add operation into redo stack.
                RedoStack.AddLast(operation);

                // Remove from undo stack.
                UndoStack.RemoveLast();

                operationNode = prev;
            }

            --ActiveTransactionIdCount;
        }

        public void Redo()
        {
            if (RedoStack.Count == 0)
            {
                return;
            }

            var operationNode = RedoStack.Last;
            var currentTransactionId = operationNode.Value.TransactionId;
            while (operationNode != null && operationNode.Value.TransactionId == currentTransactionId)
            {
                var prev = operationNode.Previous;
                var operation = RedoStack.Last.Value;
                operation.Redo();

                // Add operation into Undo stack.
                UndoStack.AddLast(operation);

                // Remove from redo stack.
                RedoStack.RemoveLast();

                operationNode = prev;
            }

            ++ActiveTransactionIdCount;
        }
    }
}
