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
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace ColoringBook.Models
{
    public static class ColoringExporter
    {
        public static async Task<ColoringBookExport> CreateExportFromUiElementAsync(
            UIElement uiElementToExport, int desiredWidth, int desiredHeight)
        {
            var export = default(ColoringBookExport);

            var rtb = new RenderTargetBitmap();
            try
            {
                await rtb.RenderAsync(uiElementToExport, desiredWidth, desiredHeight);
            }
            catch
            {
                export.WasSuccessful = false;
                return export;
            }

            export.ExportHeight = rtb.PixelHeight;
            export.ExportWidth = rtb.PixelWidth;

            export.Pixels = await rtb.GetPixelsAsync();

            export.WasSuccessful = true;

            return export;
        }

        public static async Task ExportIntoStreamAsync(UIElement uiElementToExport,
            int desiredWidth, int desiredHeight, IRandomAccessStream stream)
        {
            var export = await CreateExportFromUiElementAsync(uiElementToExport, desiredWidth, desiredHeight);

            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                (uint)export.ExportWidth, (uint)export.ExportHeight, 96, 96, export.Pixels.ToArray());

            await encoder.FlushAsync();
        }
    }
}