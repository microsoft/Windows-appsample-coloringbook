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
    public class LibraryThumbnail : Thumbnail
    {
        public LibraryThumbnail(string libraryImageName, bool isHero)
            : base(isHero)
        {
            LibraryImageName = libraryImageName;
            ThumbnailBackgroundImageName = libraryImageName +
                Tools.GetResourceString("FileType/thumbnail");
        }

        private string LibraryImageName { get; }

        public override async Task LoadThumbnailAsync()
        {
            var libraryImagesDir = await Tools.GetSubDirectoryAsync(
                await Tools.GetAssetsFolderAsync(), "LibraryImages");
            var imageDir = await Tools.GetSubDirectoryAsync(libraryImagesDir, LibraryImageName);

            // Check thumbnail file exists, image file related to
            // device resolution exists and preprocessing file exists.
            var resolutionImageFileName = LibraryImageName +
                Tools.GetResolutionTypeAsString(Tools.ResolutionType) +
                Tools.GetResourceString("FileType/png");

            var resolutionSpecificImagefile = await Tools.GetFileAsync(imageDir, resolutionImageFileName);

            var thumbnailImagefile = await Tools.GetFileAsync(imageDir, ThumbnailBackgroundImageName);

            var preprocessingFileName = LibraryImageName +
                Tools.GetResolutionTypeAsString(Tools.ResolutionType) +
                Tools.GetResourceString("FileType/preprocessing");
            var preprocessingfile = await Tools.GetFileAsync(imageDir, preprocessingFileName);

            if (resolutionSpecificImagefile != null &&
                thumbnailImagefile != null && preprocessingfile != null)
            {
                await LoadThumbnailBackgroundImageAsync(thumbnailImagefile);
            }
            else
            {
                HadError = true;
            }

            IsLoading = false;
        }

        protected override async Task LoadColoringAsync()
        {
            ColoringBookColoring coloring;

            try
            {
                coloring = await ColoringBookColoring.CreateFromTemplateAsync(LibraryImageName);
            }
            catch
            {
                coloring = null;
            }

            NavigationController.LoadColoringPage(coloring);
        }
    }
}
