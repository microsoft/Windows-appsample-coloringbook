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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI;

namespace ColoringBookPreprocessingGenerator
{
    public class Processor
    {
        public Processor(byte[] imageBytes, uint imageHeight, uint imageWidth)
        {
            ImageBytes = imageBytes;
            ImageWidth = imageWidth;
            ImageHeight = imageHeight;
        }

        private byte[] ImageBytes { get; }

        private uint ImageHeight { get; }

        private uint ImageWidth { get; }

        public async Task ConvertWhitesToTransparentAsync(StorageFile saveFile)
        {
            //Convert to grayscale with transparency
            for (var y = 0; y < ImageHeight; y++)
            {
                for (var x = 0; x < ImageWidth; x++)
                {
                    var index = (y * (int)ImageWidth + x) * 4;

                    var b = ImageBytes[index];
                    var g = ImageBytes[index + 1];
                    var r = ImageBytes[index + 2];
                    var a = ImageBytes[index + 3];

                    // Determine gray amount.
                    var alpha = a / 255.0;
                    var gv = (byte)((255 - ((r + b + g) / 3.0)) * alpha);

                    // Set to black with transparency.
                    ImageBytes[index] = 0;
                    ImageBytes[index + 1] = 0;
                    ImageBytes[index + 2] = 0;
                    // 0 is transparent (white), 255 opaque (black).
                    ImageBytes[index + 3] = gv; 
                }
            }
            await WriteImageToFileAsync(saveFile);
        }

        public async Task WriteImageToFileAsync(StorageFile saveFile)
        {
            using (var stream = await saveFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, ImageWidth, 
                    ImageHeight, 96, 96, ImageBytes);

                await encoder.FlushAsync();
            }
        }

        public async Task GeneratePreprocessingAsync(StorageFile preprocessingFile)
        {
            var lookupTable = CreateLookupTableOfImage();
            var compressedCells = CompressLookupTable(lookupTable);
            await WritePreProcessingToFileAsync(preprocessingFile, compressedCells);
        }

        private static async Task WritePreProcessingToFileAsync(StorageFile preprocessingFile,
            List<CompressedCell> compressedCells)
        {
            using (var bw = new BinaryWriter(await preprocessingFile.OpenStreamForWriteAsync()))
            {
                foreach (var cell in compressedCells)
                {
                    bw.Write(cell.CellId);
                    bw.Write(cell.NoOfConsecutiveCells);
                }
            }
        }

        public Color GetPixelColor(Point location)
        {
            var x = (int)location.X;
            var y = (int)location.Y;

            // Clamp.
            x = (int)Math.Min(ImageWidth - 1, Math.Max(0, x));
            y = (int)Math.Min(ImageHeight - 1, Math.Max(0, y));

            // Get color from BitmapImageBytes.
            var loc = (y * (int)ImageWidth + x) * 4;
            var b = ImageBytes[loc + 0];
            var g = ImageBytes[loc + 1];
            var r = ImageBytes[loc + 2];
            var a = ImageBytes[loc + 3];

            return Color.FromArgb(a, r, g, b);
        }

        private bool IsOnBoundaryForPreProcessing(Point location)
        {
            var pxc = GetPixelColor(location);

            const int grayscaleValue = 175; // out of 255
            int colorVal = (int)((pxc.R + pxc.B + pxc.G) / 3.0);
            var alpha = pxc.A / 255f;

            var isGrayEnough = (255 - colorVal) * alpha > grayscaleValue;

            return isGrayEnough;
        }

        private uint[,] CreateLookupTableOfImage()
        {
            // Structure for holding generated cells.
            //  0:     boundaries and out of bounds
            //  1...N: cellID
            uint cellId = 1;
            var lookup = new uint[ImageHeight, ImageWidth];

            var unvisitedPoints = new SortedSet<Point>(new ByPoint());
            for (var y = 0; y < ImageHeight; y++)
            {
                for (var x = 0; x < ImageWidth; x++)
                {
                    unvisitedPoints.Add(new Point(x, y));
                }
            }
            while (unvisitedPoints.Count > 0)
            {
                var p = unvisitedPoints.First();
                unvisitedPoints.Remove(unvisitedPoints.First());

                var stack = new Stack<Point>();
                stack.Push(p);

                while (stack.Count > 0)
                {
                    var pop = stack.Pop();

                    // Clamp values.
                    var x = (int)Math.Min(ImageWidth, Math.Max(pop.X, 0));
                    var y = (int)Math.Min(ImageHeight, Math.Max(pop.Y, 0));

                    if (y <= 0 || x <= 0 || y >= ImageHeight - 1 || x >= ImageWidth - 1 ||
                        IsOnBoundaryForPreProcessing(new Point(x, y)))
                    {
                        // Hit boundary or edge of image.
                        lookup[y, x] = 0;
                        continue;
                    }

                    lookup[y, x] = cellId;
                    unvisitedPoints.Remove(pop);

                    if (unvisitedPoints.Contains(new Point(x - 1, y)))
                    {
                        stack.Push(new Point(x - 1, y));
                    }
                    if (unvisitedPoints.Contains(new Point(x + 1, y)))
                    {
                        stack.Push(new Point(x + 1, y));
                    }
                    if (unvisitedPoints.Contains(new Point(x, y + 1)))
                    {
                        stack.Push(new Point(x, y + 1));
                    }
                    if (unvisitedPoints.Contains(new Point(x, y - 1)))
                    {
                        stack.Push(new Point(x, y - 1));
                    }
                }

                cellId++;
            }

            return lookup;
        }

        private List<CompressedCell> CompressLookupTable(uint[,] lookupTable)
        {
            var compressedTable = new List<CompressedCell>();
            var previousCellId = lookupTable[0, 0];
            var currentCellId = lookupTable[0, 0];
            uint consecutiveCount = 0;
            for (var j = 0; j < ImageHeight; j++)
            {
                for (var i = 0; i < ImageWidth; i++)
                {
                    currentCellId = lookupTable[j, i];
                    if (currentCellId == previousCellId)
                    {
                        consecutiveCount++;
                    }
                    else
                    {
                        compressedTable.Add(new CompressedCell(previousCellId, consecutiveCount));
                        consecutiveCount = 1;
                    }
                    previousCellId = currentCellId;
                }
            }

            // Add final compression cell as this won't be added in the for loop.
            compressedTable.Add(new CompressedCell(currentCellId, consecutiveCount));

            return compressedTable;
        }
    }

    internal class ByPoint : IComparer<Point>
    {
        public int Compare(Point p1, Point p2)
        {
            if (p1.Y > p2.Y)
            {
                return 1;
            }
            if (p1.Y < p2.Y)
            {
                return -1;
            }
            if (p1.X > p2.X)
            {
                return 1;
            }
            if (p1.X < p2.X)
            {
                return -1;
            }
            return 0;
        }
    }

    internal struct CompressedCell
    {
        public CompressedCell(uint cellId, uint noOfConsecutiveCells)
        {
            CellId = cellId;
            NoOfConsecutiveCells = noOfConsecutiveCells;
        }

        public uint CellId { get; }

        public uint NoOfConsecutiveCells { get; }
    }
}
