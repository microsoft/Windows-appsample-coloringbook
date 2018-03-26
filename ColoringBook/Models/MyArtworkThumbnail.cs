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

using System.Threading.Tasks;
using ColoringBook.Common;
using ColoringBook.Components;

namespace ColoringBook.Models
{
    public class MyArtworkThumbnail : Thumbnail
    {
        public MyArtworkThumbnail(string coloringName, bool isHero)
            : base(isHero)
        {
            ThumbnailBackgroundImageName = coloringName + Tools.GetResourceString("FileType/thumbnail");
            ColoringName = coloringName;
        }

        private string ColoringName { get; }

        public ColoringBookColoring Coloring { get; private set; }

        public override async Task LoadThumbnailAsync()
        {
            Coloring = await ColoringBookColoring.LoadThumbnailDetailsAsync(ColoringName);

            if (Coloring == null || Coloring.ColoringHasError())
            {
                SetErrorThumbnail();
            }
            else
            {
                var settings = Coloring.Settings;
                var imgfile = await Tools.GetFileAsync(settings.ColoringDirectory,
                    settings.ColoringName + Tools.GetResourceString("FileType/thumbnail"));

                await LoadThumbnailBackgroundImageAsync(imgfile);
            }

            IsLoading = false;
        }

        protected override async Task LoadColoringAsync()
        {
            if (Coloring == null || Coloring.ColoringHasError())
            {
                MarkForDelete = await ColoringBookColoring.DisplayErrorDialogAsync(ColoringName);
            }
            else
            {
                NavigationController.LoadColoringPage(Coloring);
            }
        }
    }
}
