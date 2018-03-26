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
using System.IO;
using System.Threading.Tasks;
using ColoringBook.Common;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;

namespace ColoringBook.Models
{
    public abstract class ColoringBookLibraryImage
    {
        protected ColoringBookLibraryImage(StorageFolder location, string fileName)
        {
            ImageLocation = location;
            ImageName = Path.GetFileNameWithoutExtension(fileName);
        }

        public uint Height { get; set; }

        public uint Width { get; set; }

        public string ImageName { get; }

        public StorageFolder ImageLocation { get; }

        public string FileName => ImageName + Tools.GetResourceString("FileType/png");

        public string PreprocessingName => ImageName + Tools.GetResourceString("FileType/preprocessing");

        public static async Task<bool> CheckImageExistsAsync(StorageFolder imageLocation,
            StorageFolder preprocessingLocation, string imageName, string preprocessingName)
        {
            var imgfile = await Tools.GetFileAsync(imageLocation, imageName);
            var preprocessingfile = await Tools.GetFileAsync(preprocessingLocation, preprocessingName);

            // Do not load preprocessing file now, just check that it exists.
            if (imgfile == null)
            {
                return false;
            }

            if (preprocessingfile == null)
            {
                return false;
            }

            return true;
        }

        public abstract byte[] FillCell(uint cellId, Color color, bool overrideChecks = false);

        public abstract byte[] EraseCell(uint cellId);

        public abstract void EraseAllCells();

        public abstract uint GetCorrespondingCellId(Point location);

        public abstract bool IsOnBoundary(Point location);

        // Processes the image into known "cells" to understand quickly what cell the user is coloring in.
        // This is a very long, expensive process and should only be performed once.
        // Results are compressed then written to a file and used from then on.
        public abstract void LoadPreProcessingFromFile(StorageFile preprocessingFile);

        protected abstract List<CompressedCell> ReadCompressedPreProcessingFromFile(StorageFile preprocessingFile);
    }

    public struct CompressedCell
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