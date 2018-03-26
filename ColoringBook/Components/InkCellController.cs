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
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using ColoringBook.Common;
using ColoringBook.Models;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml.Media.Imaging;

namespace ColoringBook.Components
{
    public class InkCellController : Observable
    {
        public InkCellController(ColoringBookColoring coloring)
        {
            CurrentColoring = coloring;
            CellImage = new WriteableBitmap(
                (int)CurrentColoring.TemplateImage.Width,
                (int)CurrentColoring.TemplateImage.Height);
        }

        private ColoringBookColoring CurrentColoring { get; }

        public WriteableBitmap CellImage
        {
            get => _cellImage;
            set => Set(ref _cellImage, value);
        }

        private WriteableBitmap _cellImage = new WriteableBitmap(1, 1);

        public async Task LoadImageAsync()
        {
            await LoadPreprocessingForImageAsync();
            await LoadCellsAsync();
        }

        private async Task LoadPreprocessingForImageAsync()
        {
            var templateImage = CurrentColoring.TemplateImage as ColoringBookBitmapLibraryImage;

            var preprocessingFolder = await CurrentColoring.Settings.GetLibraryImagePreprocessingLocationAsync();
            var preprocessingFile = await Tools.GetFileAsync(preprocessingFolder,
                CurrentColoring.TemplateImage.PreprocessingName);

            if (preprocessingFile != null)
            {
                templateImage.LoadPreProcessingFromFile(preprocessingFile);
            }
            else
            {
                throw new FileNotFoundException("Preprocessing file not found!");
            }
        }

        private async Task LoadCellsAsync()
        {
            var templateImage = CurrentColoring.TemplateImage as ColoringBookBitmapLibraryImage;
            await templateImage.LoadFilledCellsAsync(CurrentColoring.ColoringName);
            await ApplyFillAsync(templateImage.BitmapImageBytes);
        }

        public async Task FillCellAsync(uint cellId, Color color)
        {
            var img = CurrentColoring.TemplateImage;
            var backgroundImageBytes = img.FillCell(cellId, color);
            await ApplyFillAsync(backgroundImageBytes);
        }

        private async Task ApplyFillAsync(byte[] imageBytes)
        {
            var templateImage = CurrentColoring.TemplateImage as ColoringBookBitmapLibraryImage;
            var tffBitmap = new WriteableBitmap((int)templateImage.Width, (int)templateImage.Height);

            using (var stream = tffBitmap.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(imageBytes, 0, imageBytes.Length);
            }

            CellImage = tffBitmap;
        }

        public async Task EraseCellAsync(uint cellId)
        {
            var img = CurrentColoring.TemplateImage;
            var backgroundImageBytes = img.EraseCell(cellId);
            await ApplyFillAsync(backgroundImageBytes);
        }

        public List<InkStroke> DeleteInkWithinCell(uint cellId, InkStrokeContainer allStrokes)
        {
            var img = CurrentColoring.TemplateImage as ColoringBookBitmapLibraryImage;

            // For saving in undo/redo history.
            var erasedStrokes = new List<InkStroke>();

            // Mark strokes to delete as selected, then delete.
            foreach (var stroke in allStrokes.GetStrokes())
            {
                // All stroke points are contained within the same cell, only need to check the first.
                var strokePoint = stroke.GetInkPoints().First().Position;
                var strokeCell = img.GetCorrespondingCellId(strokePoint);

                if (strokeCell == cellId)
                {
                    stroke.Selected = true;
                    erasedStrokes.Add(stroke);
                }
            }

            allStrokes.DeleteSelected();

            return erasedStrokes;
        }

        public async Task<Dictionary<uint, Color>> ClearAllCellsAsync()
        {
            var oldCache = CurrentColoring.ClearAllCells();
            await ApplyFillAsync((CurrentColoring.TemplateImage as ColoringBookBitmapLibraryImage).BitmapImageBytes);
            return oldCache;
        }

        public async Task ApplyFilledCellsAsync(Dictionary<uint, Color> dict)
        {
            var bytes = CurrentColoring.ApplyFilledCells(dict);
            await ApplyFillAsync(bytes);
        }

        public void UpdateCurrentCell(Point location) => CurrentColoring.SetCurrentCell(location);
    }
}
