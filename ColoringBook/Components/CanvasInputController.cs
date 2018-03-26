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
using System.Threading.Tasks;
using ColoringBook.Common;
using ColoringBook.Models;
using ColoringBook.Views;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Input.Inking;
using Windows.UI.Input.Inking.Core;

namespace ColoringBook.Components
{
    public class CanvasInputController
    {
        public CanvasInputController(CanvasElements canvasElements)
        {
            CanvasElements = canvasElements;
            Dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            SetupInkCanvasInput();
        }

        ~CanvasInputController() => CleanupInkCanvasInput();

        public event EventHandler PointerPressed;

        public event EventHandler PointerReleased;

        public event EventHandler<StrokesCollectedEventArgs> StrokesCollected;

        public event EventHandler<CellOperationEventArgs> FillCell;

        public event EventHandler<CellOperationEventArgs> EraseCell;

        public event EventHandler<PointerPositionChangedEventArgs> StrokeEraserMoved;

        public event EventHandler<RightClickPanChangedEventArgs> RightPanMoved;

        public event EventHandler<StrokeManufacturedEventArgs> StrokeManufactured;

        public ColoringBookColoring CurrentColoring { get; set; }

        public InkCellController InkCellController { get; set; }

        public DrawingTool DrawingTool
        {
            get => _drawingTool;
            set
            {
                // Store eraser mode so we can erase properly with the back of the pen.
                if ((value == DrawingTool.Eraser) || (value == DrawingTool.EraseCell))
                {
                    LastEraserTool = value;
                }

                _drawingTool = value;
            }
        }

        private DrawingTool _drawingTool;

        private CanvasElements CanvasElements { get; }

        private InkSynchronizer InkSynchronizer { get; set; }

        private bool IsErasing { get; set; }

        private bool IsRightClickPanOn { get; set; }

        private bool IsEraserTip { get; set; }

        private ref Point LastPosition => ref _lastPosition;

        private Point _lastPosition;

        private Point EraserLastPoint { get; set; }

        private Point CellPressedPoint { get; set; }

        private CoreWetStrokeUpdateSource CoreWetStrokeUpdateSource { get; set; }

        private CoreDispatcher Dispatcher { get; }

        // Needed for real time ink processing.
        private List<InkPoint> StrokeToAddAfterDispositionCompleted { get; set; }
            = new List<InkPoint>();

        private ref bool InkingInProgress => ref _inkingInProgress;

        private volatile bool _inkingInProgress;

        private ref bool ShouldResetCurrentCell => ref _shouldResetCurrentCell;

        private volatile bool _shouldResetCurrentCell = true;

        private InkPoint LastPlacedInkPoint { get; set; }

        private DrawingTool LastEraserTool { get; set; } = DrawingTool.Eraser;

        private void SetupInkCanvasInput()
        {
            var inkPresenter = CanvasElements.InkCanvas.InkPresenter;

            // Initialize defaults for the InkCanvas.
            inkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Mouse | CoreInputDeviceTypes.Pen;
            inkPresenter.InputProcessingConfiguration.RightDragAction = InkInputRightDragAction.LeaveUnprocessed;

            // Keep for updating when starting / continuing / finishing pen drawing.
            CoreWetStrokeUpdateSource = CoreWetStrokeUpdateSource.Create(inkPresenter);
            CoreWetStrokeUpdateSource.WetStrokeStarting += CoreWetStrokeUpdateSource_WetStrokeOnLay;
            CoreWetStrokeUpdateSource.WetStrokeContinuing += CoreWetStrokeUpdateSource_WetStrokeOnLay;
            CoreWetStrokeUpdateSource.WetStrokeStopping += CoreWetStrokeUpdateSource_WetStrokeOnLay;

            // Custom Drying.
            InkSynchronizer = inkPresenter.ActivateCustomDrying();
            inkPresenter.StrokesCollected += InkPresenter_StrokesCollected;

            // Pointer pressed for new stroke.
            inkPresenter.StrokeInput.StrokeStarted += StrokeInput_PointerPressed;
            inkPresenter.StrokeInput.StrokeEnded += StrokeInput_PointerReleased;

            // Eraser logic for custom ink.
            var unprocessedInput = inkPresenter.UnprocessedInput;
            unprocessedInput.PointerPressed += UnprocessedInput_PointerPressed;
            unprocessedInput.PointerMoved += UnprocessedInput_PointerMoved;
            unprocessedInput.PointerReleased += UnprocessedInput_PointerReleased;
            unprocessedInput.PointerExited += UnprocessedInput_PointerExited;
            unprocessedInput.PointerLost += UnprocessedInput_PointerLost;
        }

        private void CleanupInkCanvasInput()
        {
            // Keep for updating when starting / continuing / finishing pen drawing.
            CoreWetStrokeUpdateSource.WetStrokeStarting -= CoreWetStrokeUpdateSource_WetStrokeOnLay;
            CoreWetStrokeUpdateSource.WetStrokeContinuing -= CoreWetStrokeUpdateSource_WetStrokeOnLay;
            CoreWetStrokeUpdateSource.WetStrokeStopping -= CoreWetStrokeUpdateSource_WetStrokeOnLay;

            var inkPresenter = CanvasElements.InkCanvas.InkPresenter;

            inkPresenter.StrokesCollected -= InkPresenter_StrokesCollected;

            // Pointer pressed for new stroke.
            inkPresenter.StrokeInput.StrokeStarted -= StrokeInput_PointerPressed;
            inkPresenter.StrokeInput.StrokeEnded -= StrokeInput_PointerReleased;

            // Eraser logic for custom ink.
            var unprocessedInput = inkPresenter.UnprocessedInput;
            unprocessedInput.PointerPressed -= UnprocessedInput_PointerPressed;
            unprocessedInput.PointerMoved -= UnprocessedInput_PointerMoved;
            unprocessedInput.PointerReleased -= UnprocessedInput_PointerReleased;
            unprocessedInput.PointerExited -= UnprocessedInput_PointerExited;
            unprocessedInput.PointerLost -= UnprocessedInput_PointerLost;
        }

        private void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            var newCollectedStrokes = InkSynchronizer.BeginDry().Select(stroke => stroke.Clone()).ToList();
            InkSynchronizer.EndDry();
            StrokesCollected?.Invoke(this, new StrokesCollectedEventArgs
            {
                CollectedStrokes = newCollectedStrokes
            });
        }

        private void AddPreviousWetStrokeOnLayStroke(Point lastAddedPoint,
            CoreWetStrokeUpdateEventArgs args)
        {
            var stroke = StrokeToAddAfterDispositionCompleted;
            if (stroke == null || stroke.Count <= 0)
            {
                return;
            }

            // Insert the final stroke's ink points into the front of NewInkPoints, in reverse order.
            // Only add to the NewInkPoints if the strokes should join together.
            if (stroke.Last().Position == lastAddedPoint)
            {
                foreach (var inkpoint in stroke)
                {
                    if (!CurrentColoring.IsWithinUserCurrentCell(inkpoint.Position))
                    {
                        break;
                    }

                    args.NewInkPoints.Insert(0, inkpoint);
                }
            }

            // Otherwise manufacture the stroke manually.
            else
            {
                ManufactureStrokeAsync(stroke).ContinueWithoutWaiting();
            }

            StrokeToAddAfterDispositionCompleted = null;
        }

        private void RemoveInkPointsOutsideOfUsersCell(IList<InkPoint> inkPoints)
        {
            for (var i = 0; i < inkPoints.Count; i++)
            {
                if (CurrentColoring.IsWithinUserCurrentCell(inkPoints[i].Position))
                {
                    continue;
                }

                // Outside of users cell, remove all points from this point onwards.
                for (var j = inkPoints.Count - 1; j >= 0; j--)
                {
                    inkPoints.RemoveAt(j);
                }
            }
        }

        private void ProcessNewInkPoints(CoreWetStrokeUpdateEventArgs args)
        {
            if (args.NewInkPoints.Count <= 0)
            {
                return;
            }

            // 1. Collect new ink points from args.NewInkPoints.
            // 2. Check if the user's cell should be reset.
            // 3. Split into strokes.
            //   * This will take the new points, and the last known point
            //     and create a list of all strokes that are within the users cell.
            //   * Note: This may not be one contiguous stroke, if the user colors inside,
            //     outside, and back inside a cell this will create several different
            //     inkstrokes as required.
            // 4. For each new stroke that needs to be drawn on the canvas, the new strokes
            //   either need to be joined to the previous stroke, manually manufactured or
            //   marked to be joined onto the next NewPoint in the next ProcessNewInkPoints event.
            if (ShouldResetCurrentCell)
            {
                if (CurrentColoring.TemplateImage.IsOnBoundary(args.NewInkPoints.First().Position))
                {
                    args.NewInkPoints.Clear();
                    args.Disposition = CoreWetStrokeDisposition.Completed;
                    return;
                }

                InkCellController.UpdateCurrentCell(args.NewInkPoints.First().Position);
                LastPlacedInkPoint = args.NewInkPoints.First();
                ShouldResetCurrentCell = false;
            }

            // From first point -> last point, create InkStrokes that are within the users cell.
            var inkStrokes = InkStrokeMathHelper.SplitIntoStrokes(args.NewInkPoints, LastPlacedInkPoint, CurrentColoring);

            var lastAddedPoint = LastPlacedInkPoint.Position;
            LastPlacedInkPoint = args.NewInkPoints.Last();
            args.NewInkPoints.Clear();

            // Check the strokes to be added from the previous WetStrokeOnLay event.
            AddPreviousWetStrokeOnLayStroke(lastAddedPoint, args);

            // If there were no ink strokes, and all points were removed for being outside
            // the user's cell, mark stroke as completed.
            if (inkStrokes?.Count <= 0 && args.NewInkPoints.Count == 0)
            {
                args.Disposition = CoreWetStrokeDisposition.Completed;
            }

            // Either join strokes to currently existing ink points or manufacture as new strokes.
            // Note: new stroke may be required to join onto next WetStrokeOnLay event.
            if (inkStrokes == null)
            {
                return;
            }

            foreach (var stroke in inkStrokes)
            {
                if ((stroke.First().Position == lastAddedPoint) &&
                    args.Disposition != CoreWetStrokeDisposition.Completed)
                {
                    foreach (var newInkPoint in stroke)
                    {
                        args.NewInkPoints.Add(newInkPoint);
                    }
                }
                else
                {
                    args.Disposition = CoreWetStrokeDisposition.Completed;

                    if (StrokeToAddAfterDispositionCompleted != null &&
                        StrokeToAddAfterDispositionCompleted.Count > 0)
                    {
                        // Merge connected strokes.
                        if (stroke.Count > 0 &&
                            StrokeToAddAfterDispositionCompleted.Last().Position ==
                                stroke.First().Position)
                        {
                            stroke.InsertRange(0, StrokeToAddAfterDispositionCompleted);
                        }
                        else
                        {
                            ManufactureStrokeAsync(StrokeToAddAfterDispositionCompleted)
                                .ContinueWithoutWaiting();
                        }
                    }

                    StrokeToAddAfterDispositionCompleted = stroke;
                }

                lastAddedPoint = stroke.Last().Position;
            }
        }

        private void CoreWetStrokeUpdateSource_WetStrokeOnLay(CoreWetStrokeUpdateSource sender,
            CoreWetStrokeUpdateEventArgs args)
        {
            if (InkingInProgress)
            {
                ProcessNewInkPoints(args);
            }
            else
            {
                args.NewInkPoints.Clear();
            }
        }

        private void StrokeInput_PointerPressed(InkStrokeInput sender, PointerEventArgs args)
        {
            args.Handled = true;
            InkingInProgress = true;
            ShouldResetCurrentCell = true;
            PointerPressed?.Invoke(this, EventArgs.Empty);
        }

        private void StrokeInput_PointerReleased(InkStrokeInput sender, PointerEventArgs args)
        {
            InkingInProgress = false;
            PointerReleased?.Invoke(this, EventArgs.Empty);
            args.Handled = true;
        }

        private void UnprocessedInput_PointerPressed(InkUnprocessedInput sender, PointerEventArgs args)
        {
            var ptrPt = args.CurrentPoint;
            if (ptrPt.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
            {
                IsRightClickPanOn = true;
                LastPosition = args.CurrentPoint.RawPosition;
                return;
            }

            IsEraserTip = ptrPt.Properties.IsEraser;
            var isStrokeErase = (IsEraserTip && (LastEraserTool == DrawingTool.Eraser)) ||
                                (DrawingTool == DrawingTool.Eraser);
            var isCellErase = (IsEraserTip && (LastEraserTool == DrawingTool.EraseCell)) ||
                              (DrawingTool == DrawingTool.EraseCell);

            if (isStrokeErase)
            {
                args.Handled = true;
                EraserLastPoint = ptrPt.Position;
                IsErasing = true;
            }
            else if (isCellErase || DrawingTool == DrawingTool.FillCell)
            {
                CellPressedPoint = ptrPt.Position;
            }

            PointerPressed?.Invoke(this, EventArgs.Empty);
        }

        private void UnprocessedInput_PointerMoved(InkUnprocessedInput sender, PointerEventArgs args)
        {
            if (IsRightClickPanOn)
            {
                var newPosition = args.CurrentPoint.Position;

                double dX = newPosition.X - LastPosition.X;
                double dY = newPosition.Y - LastPosition.Y;

                double? horizontal = CanvasElements.CanvasScrollViewer.HorizontalOffset - dX;
                double? vertical = CanvasElements.CanvasScrollViewer.VerticalOffset - dY;
                RightPanMoved?.Invoke(this, new RightClickPanChangedEventArgs
                {
                    HorizontalOffset = horizontal.Value,
                    VerticalOffset = vertical.Value
                });

                LastPosition.X += dX;
                LastPosition.Y += dY;
                return;
            }

            var isStrokeErase = (IsEraserTip && (LastEraserTool == DrawingTool.Eraser)) ||
                                (DrawingTool == DrawingTool.Eraser);
            if (isStrokeErase)
            {
                if (!IsErasing)
                {
                    return;
                }

                StrokeEraserMoved?.Invoke(this, new PointerPositionChangedEventArgs
                {
                    PreviousPoint = EraserLastPoint,
                    CurrentPoint = args.CurrentPoint.Position
                });

                EraserLastPoint = args.CurrentPoint.Position;
            }
        }

        private void UnprocessedInput_PointerReleased(InkUnprocessedInput sender, PointerEventArgs args)
        {
            var ptrPt = args.CurrentPoint;
            if (ptrPt.Properties.PointerUpdateKind == PointerUpdateKind.RightButtonReleased)
            {
                IsRightClickPanOn = false;
                return;
            }

            var isStrokeErase = (IsEraserTip && (LastEraserTool == DrawingTool.Eraser)) ||
                                (DrawingTool == DrawingTool.Eraser);
            var isCellErase = (IsEraserTip && (LastEraserTool == DrawingTool.EraseCell)) ||
                              (DrawingTool == DrawingTool.EraseCell);

            if (isStrokeErase)
            {
                if (IsErasing)
                {
                    args.Handled = true;
                }

                IsErasing = false;
            }
            else
            {
                var operationArgs = new CellOperationEventArgs
                {
                    FirstPoint = CellPressedPoint,
                    CurrentPoint = args.CurrentPoint.Position
                };
                if (isCellErase)
                {
                    EraseCell?.Invoke(this, operationArgs);
                }
                else if (DrawingTool == DrawingTool.FillCell)
                {
                    FillCell?.Invoke(this, operationArgs);
                }
            }

            PointerReleased?.Invoke(this, EventArgs.Empty);
            IsEraserTip = false;
        }

        private void UnprocessedInput_PointerLost(InkUnprocessedInput sender, PointerEventArgs args)
        {
            var isEraserTip = args.CurrentPoint.Properties.IsEraser;
            var isStrokeErase = (isEraserTip && (LastEraserTool == DrawingTool.Eraser)) ||
                                (DrawingTool == DrawingTool.Eraser);
            if (isStrokeErase)
            {
                if (IsErasing)
                {
                    args.Handled = true;
                }

                IsErasing = false;
            }

            IsRightClickPanOn = false;
            IsEraserTip = false;
        }

        private void UnprocessedInput_PointerExited(InkUnprocessedInput sender, PointerEventArgs args)
        {
            var isEraserTip = args.CurrentPoint.Properties.IsEraser;
            var isStrokeErase = (isEraserTip && (LastEraserTool == DrawingTool.Eraser)) ||
                                (DrawingTool == DrawingTool.Eraser);
            if (isStrokeErase)
            {
                if (IsErasing)
                {
                    args.Handled = true;
                }

                IsErasing = true;
            }

            IsRightClickPanOn = false;
            IsEraserTip = false;
        }

        private async Task ManufactureStrokeAsync(List<InkPoint> stroke)
        {
            if (stroke == null || stroke.Count <= 0 || !InkStrokeMathHelper.IsManufacturedStrokeLongEnough(stroke))
            {
                return;
            }

            var strokeClone = new List<InkPoint>(stroke);

            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                var newStroke = InkStrokeMathHelper.BuildInkStroke(strokeClone);

                if (newStroke == null)
                {
                    return;
                }

                newStroke.DrawingAttributes = CanvasElements.InkCanvas.InkPresenter.CopyDefaultDrawingAttributes();

                StrokeManufactured?.Invoke(this, new StrokeManufacturedEventArgs { Stroke = newStroke });
            });
        }
    }
}
