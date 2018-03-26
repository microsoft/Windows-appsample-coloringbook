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
using System.Threading.Tasks;
using ColoringBook.Common;
using ColoringBook.FileIO;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml.Media.Imaging;

namespace ColoringBook.Models
{
    public class ColoringBookBitmapLibraryImage : ColoringBookLibraryImage
    {
        private ColoringBookBitmapLibraryImage(StorageFolder location, string fileName)
            : base(location, fileName)
        {
        }

        public Dictionary<uint, Color> CellColorCache { get; private set; }
            = new Dictionary<uint, Color>();

        private Dictionary<uint, BoundingBox> BoundingBoxLookup { get; set; }

        private bool IsTableComputed { get; set; }

        private uint[,] LookupTable { get; set; }

        private TapToFillReader TapToFillReader { get; } = new TapToFillReader();

        private TapToFillWriter TapToFillWriter { get; } = new TapToFillWriter();

        public byte[] BitmapImageBytes { get; set; }

        public BitmapImage LibraryBitmapImage { get; set; }

        public override uint GetCorrespondingCellId(Point location) =>
            !IsTableComputed || location.Y <= 0 || location.X <= 0 ||
                location.Y >= Height - 1 || location.X >= Width - 1 ?
                0 : LookupTable[(int)location.Y, (int)location.X];

        public override bool IsOnBoundary(Point location) =>
            GetCorrespondingCellId(location) == 0;

        public static async Task<ColoringBookBitmapLibraryImage> LoadImageFromFileAsync(StorageFolder imageLocation,
            StorageFolder preprocessingLocation, string imageName, string preprocessingName)
        {
            var templateImageObj = new ColoringBookBitmapLibraryImage(imageLocation, imageName);

            var imgfile = await Tools.GetFileAsync(imageLocation, imageName);
            var preprocessingfile = await Tools.GetFileAsync(preprocessingLocation, preprocessingName);

            // Do not load preprocessing file now, just check that it exists.
            if (imgfile == null)
            {
                throw new FileNotFoundException("Coloring image file not found");
            }

            if (preprocessingfile == null)
            {
                throw new FileNotFoundException("Coloring image preprocessing not found");
            }

            using (var fileStream = await imgfile.OpenAsync(FileAccessMode.Read))
            {
                templateImageObj.LibraryBitmapImage = new BitmapImage();

                var dec = await BitmapDecoder.CreateAsync(fileStream);
                templateImageObj.LibraryBitmapImage.DecodePixelWidth = (int)dec.PixelWidth;

                var data = await dec.GetPixelDataAsync();

                templateImageObj.BitmapImageBytes = data.DetachPixelData();
                templateImageObj.Width = dec.PixelWidth;
                templateImageObj.Height = dec.PixelHeight;

                await templateImageObj.LibraryBitmapImage.SetSourceAsync(fileStream);
            }

            return templateImageObj;
        }

        public Color GetColorOfCell(uint cellId)
        {
            try
            {
                return CellColorCache[cellId];
            }
            catch
            {
                return Constants.ClearColor;
            }
        }

        private void FillPixels(uint cellId, Color color)
        {
            var bbox = BoundingBoxLookup[cellId];

            // Using bbox to reduce the search space.
            for (var y = bbox.Y1; y <= bbox.Y2; y++)
            {
                for (var x = bbox.X1; x <= bbox.X2; x++)
                {
                    var p = new Point(x, y);
                    if (GetCorrespondingCellId(p) == cellId)
                    {
                        var index = (((int)y * (int)Width) + (int)x) * 4;

                        BitmapImageBytes[index + 0] = color.B;
                        BitmapImageBytes[index + 1] = color.G;
                        BitmapImageBytes[index + 2] = color.R;
                        BitmapImageBytes[index + 3] = color.A;
                    }
                }
            }
        }

        public override byte[] FillCell(uint cellId, Color color, bool overrideChecks = false)
        {
            if (!overrideChecks)
            {
                // Check if already filled to this color.
                if (CellColorCache.ContainsKey(cellId) && CellColorCache[cellId] == color)
                {
                    return BitmapImageBytes;
                }

                // Don't perform flood fill if already clear color; that is,
                // not in the knowledge  cache and color is the Clear color.
                if (!CellColorCache.ContainsKey(cellId) && color == Constants.ClearColor)
                {
                    return BitmapImageBytes;
                }
            }

            FillPixels(cellId, color);

            if (color == Constants.ClearColor)
            {
                CellColorCache.Remove(cellId);
            }
            else
            {
                CellColorCache[cellId] = color;
            }

            return BitmapImageBytes;
        }

        public override byte[] EraseCell(uint cellId) => FillCell(cellId, Constants.ClearColor);

        public override void EraseAllCells()
        {
            CellColorCache = new Dictionary<uint, Color>();
            BitmapImageBytes = new byte[Height * Width * 4];
        }

        public byte[] ApplyFilledCells(Dictionary<uint, Color> dict)
        {
            CellColorCache = dict;
            ApplyCacheAfterLoading();
            return BitmapImageBytes;
        }

        public void ApplyCacheAfterLoading()
        {
            foreach (var item in CellColorCache)
            {
                FillPixels(item.Key, item.Value);
            }
        }

        public async Task LoadFilledCellsAsync(string coloringName)
        {
            var coloringsDirectory = await Tools.GetColoringsDirectoryAsync();
            var coloringDirectory = await Tools.GetSubDirectoryAsync(coloringsDirectory, coloringName);

            try
            {
                var fileName = coloringName + Tools.GetResourceString("FileType/tapToFillType");
                CellColorCache = await TapToFillReader.ReadAsync(coloringDirectory, fileName);
                ApplyCacheAfterLoading();
            }
            catch
            {
                // ignored
            }
        }

        public override void LoadPreProcessingFromFile(StorageFile preprocessingFile)
        {
            // Load the compressed cells from binary format.
            var compressedCells = ReadCompressedPreProcessingFromFile(preprocessingFile);

            // Convert compressed cells to 2D uint array and store.
            UncompressCompressedCells(compressedCells);

            IsTableComputed = true;
        }

        protected override List<CompressedCell> ReadCompressedPreProcessingFromFile(
            StorageFile preprocessingFile)
        {
            // Load the cells in compressed format.
            var compressedTable = new List<CompressedCell>();

            // For speed, load entire file into memory then process.
            var file = File.ReadAllBytes(preprocessingFile.Path);

            for (int i = 0; i < file.Length; i += 8)
            {
                var cellId = BitConverter.ToUInt32(file, i);
                var amount = BitConverter.ToUInt32(file, i + 4);
                compressedTable.Add(new CompressedCell(cellId, amount));
            }

            return compressedTable;
        }

        public async Task SaveFilledCellsToFileAsync(string coloringName, StorageFolder coloringDirectory)
        {
            var fname = coloringName + Tools.GetResourceString("FileType/tapToFillType");
            var data = CellColorCache;
            await TapToFillWriter.WriteAsync(coloringDirectory, fname, data);
        }

        private void UncompressCompressedCells(List<CompressedCell> compressedCells)
        {
            LookupTable = new uint[Height, Width];
            BoundingBoxLookup = new Dictionary<uint, BoundingBox>();

            var i = 0;
            foreach (var cell in compressedCells)
            {
                for (var ii = 0; ii < cell.NoOfConsecutiveCells; ii++)
                {
                    var x = (uint)(i % Width);
                    var y = (uint)(i / Width);

                    if (y >= Height)
                    {
                        continue;
                    }

                    LookupTable[y, x] = cell.CellId;

                    var r = default(BoundingBox);
                    if (BoundingBoxLookup.ContainsKey(cell.CellId))
                    {
                        r = BoundingBoxLookup[cell.CellId];
                        r.X1 = Math.Min(x, r.X1);
                        r.Y1 = Math.Min(y, r.Y1);
                        r.X2 = Math.Max(x, r.X2);
                        r.Y2 = Math.Max(y, r.Y2);
                    }
                    else
                    {
                        r.X1 = x;
                        r.Y1 = y;
                        r.X2 = x;
                        r.Y2 = y;
                    }

                    BoundingBoxLookup[cell.CellId] = r;

                    i++;
                }
            }
        }

        private struct BoundingBox
        {
            public uint X1 { get; set; }

            public uint X2 { get; set; }

            public uint Y1 { get; set; }

            public uint Y2 { get; set; }
        }
    }
}