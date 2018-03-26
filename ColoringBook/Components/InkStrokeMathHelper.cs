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
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using ColoringBook.Common;
using ColoringBook.Models;
using Windows.Foundation;
using Windows.UI.Input.Inking;

namespace ColoringBook.Components
{
    public static class InkStrokeMathHelper
    {
        public static List<List<InkPoint>> SplitIntoStrokes(IEnumerable<InkPoint> newPoints,
            InkPoint previousPoint, ColoringBookColoring coloring)
        {
            var potentialStrokes = new List<List<InkPoint>>();
            var newStroke = new List<InkPoint>();

            foreach (var newPoint in newPoints)
            {
                var startPoint = previousPoint;
                var finalPoint = newPoint;

                var lastAddedPoint = new Point(-1, -1);

                var points = GetPointsBetweenPoints(startPoint.Position, finalPoint.Position);
                var boundaryPoints = GetBoundaryPoints(points, coloring);

                var boundaryLines = GetLinesFromPoints(startPoint.Position, finalPoint.Position, boundaryPoints, coloring);

                if (boundaryLines?.Count > 0)
                {
                    foreach (var line in boundaryLines)
                    {
                        // Self contained stroke.
                        if (line.IsFromBoundary && line.IsToBoundary)
                        {
                            // Save old line.
                            if (newStroke.Count > 0)
                            {
                                potentialStrokes.Add(newStroke);
                                newStroke = new List<InkPoint>();
                            }

                            // Save self contained line.
                            if (line.Start != lastAddedPoint)
                            {
                                var pressure = InterpolatePressure(startPoint, finalPoint, line.Start);
                                newStroke.Add(new InkPoint(line.Start, pressure));
                                lastAddedPoint = line.Start;
                            }

                            if (line.End != lastAddedPoint)
                            {
                                var pressure = InterpolatePressure(startPoint, finalPoint, line.End);
                                newStroke.Add(new InkPoint(line.End, pressure));
                                lastAddedPoint = line.End;
                            }

                            if (newStroke.Count > 0)
                            {
                                potentialStrokes.Add(newStroke);
                            }

                            // Reset list.
                            newStroke = new List<InkPoint>();
                        }
                        else if (line.IsFromBoundary)
                        {
                            // Save old line.
                            if (newStroke.Count > 0)
                            {
                                potentialStrokes.Add(newStroke);
                            }

                            newStroke = new List<InkPoint>();

                            // Begin new one.
                            if (line.Start != lastAddedPoint)
                            {
                                var pressure = InterpolatePressure(startPoint, finalPoint, line.Start);
                                newStroke.Add(new InkPoint(line.Start, pressure));
                                lastAddedPoint = line.Start;
                            }

                            if (line.End != lastAddedPoint)
                            {
                                var pressure = InterpolatePressure(startPoint, finalPoint, line.End);
                                newStroke.Add(new InkPoint(line.End, pressure));
                                lastAddedPoint = line.End;
                            }
                        }
                        else if (line.IsToBoundary)
                        {
                            // Add points - check if previous point is the same as start point.
                            if ((newStroke.Count == 0 || newStroke.Last().Position != line.Start) &&
                                line.Start != lastAddedPoint)
                            {
                                var pressure = InterpolatePressure(startPoint, finalPoint, line.Start);
                                newStroke.Add(new InkPoint(line.Start, pressure));
                                lastAddedPoint = line.Start;
                            }

                            if (line.End != lastAddedPoint)
                            {
                                var pressure = InterpolatePressure(startPoint, finalPoint, line.End);
                                newStroke.Add(new InkPoint(line.End, pressure));
                                lastAddedPoint = line.End;
                            }

                            // End stroke.
                            if (newStroke.Count > 0)
                            {
                                potentialStrokes.Add(newStroke);
                            }

                            // Start new stroke.
                            newStroke = new List<InkPoint>();
                        }
                    }
                }
                else
                {
                    // Normal stroke completely within user cell.
                    if (coloring.IsWithinUserCurrentCell(startPoint.Position) &&
                        coloring.IsWithinUserCurrentCell(finalPoint.Position))
                    {
                        if ((newStroke.Count == 0 || newStroke.Last() != startPoint) &&
                            startPoint.Position != lastAddedPoint)
                        {
                            newStroke.Add(startPoint);
                            lastAddedPoint = startPoint.Position;
                        }

                        if (finalPoint.Position != lastAddedPoint)
                        {
                            newStroke.Add(finalPoint);
                            lastAddedPoint = finalPoint.Position;
                        }
                    }

                    // Start, end or both of stroke is not within boundary and didn't trigger previous conditions.
                    else
                    {
                        // Save old stroke.
                        if (newStroke.Count > 0)
                        {
                            potentialStrokes.Add(newStroke);
                            newStroke = new List<InkPoint>();
                        }

                        if (startPoint.Position != lastAddedPoint &&
                            coloring.IsWithinUserCurrentCell(startPoint.Position))
                        {
                            newStroke.Add(startPoint);
                            lastAddedPoint = startPoint.Position;
                        }

                        if (lastAddedPoint != finalPoint.Position &&
                            coloring.IsWithinUserCurrentCell(finalPoint.Position))
                        {
                            newStroke.Add(finalPoint);
                            lastAddedPoint = finalPoint.Position;
                        }
                    }
                }

                previousPoint = newPoint;
            }

            if (newStroke.Count > 0)
            {
                potentialStrokes.Add(newStroke);
            }

            return potentialStrokes;
        }

        public static bool IsManufacturedStrokeLongEnough(List<InkPoint> stroke)
        {
            var a = stroke.First().Position;
            var b = stroke.Last().Position;

            var dist = PointDistance(a, b);

            return dist >= Constants.MinimumManufacturedStrokeLength;
        }

        private static InkStrokeBuilder InkStrokeBuilder { get; } = new InkStrokeBuilder();

        public static InkStroke BuildInkStroke(IEnumerable<InkPoint> inkPoints)
        {
            try
            {
                return InkStrokeBuilder.CreateStrokeFromInkPoints(inkPoints, Matrix3x2.Identity);
            }
            catch (ArgumentException e)
            {
                Debug.WriteLine(e.Message);
            }

            return null;
        }

        // For more info, see https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm
        private static List<Point> GetPointsBetweenPoints(Point start, Point end)
        {
            const double incrementAmount = 1.0;
            var points = new List<Point>();

            // start == end
            if ((int)start.X == (int)end.X && (int)start.Y == (int)end.Y)
            {
                points.Add(end);
                return points;
            }

            // verticle line
            if ((int)start.X == (int)end.X)
            {
                var sign = Math.Sign(end.Y - start.Y);
                for (var yval = start.Y; sign * yval <= sign * end.Y; yval += incrementAmount * sign)
                {
                    points.Add(new Point(start.X, yval));
                }

                return points;
            }

            var dx = end.X - start.X;
            var dy = end.Y - start.Y;

            var xSign = Math.Sign(dx);
            var ySign = Math.Sign(dy);

            var mm = Math.Abs(dy / dx);
            var cm = -incrementAmount;

            var y = start.Y;
            for (var x = start.X; xSign * x <= xSign * end.X; x += incrementAmount * xSign)
            {
                points.Add(new Point(x, y));
                cm += mm;

                while (cm > 0.0)
                {
                    y += incrementAmount * ySign;

                    // Check that it hasn't gone over the edge.
                    if (ySign * y < ySign * end.Y)
                    {
                        points.Add(new Point(x, y));
                    }

                    cm -= incrementAmount;
                }
            }

            return points;
        }

        // Extracts the points in a list that are on either side of a boundary.
        private static List<BoundaryPoint> GetBoundaryPoints(List<Point> points, ColoringBookColoring coloring)
        {
            if (points == null || points.Count == 0)
            {
                return null;
            }

            var boundaryPoints = new List<BoundaryPoint>();
            var previouslyOnBoundary = coloring.TemplateImage.IsOnBoundary(points.First());
            var previousPoint = points.First();

            foreach (var point in points)
            {
                var currentlyOnBoundary = coloring.TemplateImage.IsOnBoundary(point);

                if (currentlyOnBoundary && !previouslyOnBoundary)
                {
                    boundaryPoints.Add(new BoundaryPoint(false, previousPoint));
                }

                if (!currentlyOnBoundary && previouslyOnBoundary)
                {
                    boundaryPoints.Add(new BoundaryPoint(true, point));
                }

                previousPoint = point;
                previouslyOnBoundary = currentlyOnBoundary;
            }

            return boundaryPoints;
        }

        private static List<MyInkLine> GetLinesFromPoints(Point start, Point end,
            List<BoundaryPoint> boundaryPoints, ColoringBookColoring coloring)
        {
            var lines = new List<MyInkLine>();

            if (boundaryPoints == null || boundaryPoints.Count == 0)
            {
                return null;
            }

            var didStartReachable = coloring.IsWithinUserCurrentCell(start);
            var didFinishReachable = coloring.IsWithinUserCurrentCell(end);

            // Set start to be the previous point. If the point is within the cell, equate to being "after" a boundary.
            // We create a line if consecutive after -> before && (after || before) is within users cell (both should be).
            var startBPoint = new BoundaryPoint(didStartReachable, start);
            var previousPoint = startBPoint;

            // Set end point as reverse and we would want a line *to* there to *from* there as with start.
            var endBPoint = new BoundaryPoint(!didFinishReachable, end);
            boundaryPoints.Add(endBPoint);

            foreach (var currentPoint in boundaryPoints)
            {
                // if after -> before
                if (previousPoint.IsAfterBoundary && !currentPoint.IsAfterBoundary &&
                    coloring.IsWithinUserCurrentCell(currentPoint.Point))
                {
                    if (previousPoint.Equals(startBPoint))
                    {
                        lines.Add(new MyInkLine(previousPoint.Point, currentPoint.Point, false, true));
                    }
                    else if (currentPoint.Equals(endBPoint))
                    {
                        // Boundary to end.
                        lines.Add(new MyInkLine(previousPoint.Point, currentPoint.Point, true, false));
                    }
                    else
                    {
                        // It must be boundary to boundary.
                        lines.Add(new MyInkLine(previousPoint.Point, currentPoint.Point, true, true));
                    }
                }

                previousPoint = currentPoint;
            }

            return lines;
        }

        private static float InterpolatePressure(InkPoint start, InkPoint end, Point p)
        {
            var distanceStartEnd = PointDistance(start.Position, end.Position);
            var distanceStartP = PointDistance(start.Position, p);

            var percentageAlongLine = distanceStartP / distanceStartEnd;

            var pressure = (end.Pressure * percentageAlongLine) + (start.Pressure * (1 - percentageAlongLine));

            return (float)pressure;
        }

        private static double PointDistance(Point a, Point b)
        {
            var dx = Math.Abs(a.X - b.X);
            var dy = Math.Abs(a.Y - b.Y);

            var dist = Math.Sqrt((dx * dx) + (dy * dy));

            return dist;
        }

        private struct BoundaryPoint
        {
            public BoundaryPoint(bool isAfterBoundary, Point point)
            {
                IsAfterBoundary = isAfterBoundary;
                Point = point;
            }

            // Indicates whether this point occurs after or before the boundary.
            public bool IsAfterBoundary { get; }

            public Point Point { get; }
        }

        private struct MyInkLine
        {
            public MyInkLine(Point start, Point end, bool isFromBoundary, bool isToBoundary)
            {
                Start = start;
                End = end;
                IsFromBoundary = isFromBoundary;
                IsToBoundary = isToBoundary;
            }

            public Point Start { get; }

            public Point End { get; }

            public bool IsFromBoundary { get; }

            public bool IsToBoundary { get; }
        }
    }
}
