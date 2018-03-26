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
using Windows.Storage.Search;
using Windows.UI.Xaml.Controls;

namespace ColoringBook.Views
{
    public sealed class LibraryPivotItem : ColoringBookPivotItem
    {
        public LibraryPivotItem()
            : base()
        {
            Header = Tools.GetResourceString("LibraryPivotItem/Header");
            LoadThumbnailGroupAsync().ContinueWithoutWaiting();
        }

        public async override Task LoadThumbnailGroupAsync()
        {
            List<string> libraryImages = await GetFileNamesAsync();

            foreach (var imageName in libraryImages)
            {
                var thumbnail = new LibraryThumbnail(imageName, false);
                ViewModel.ThumbnailsSource.Add(thumbnail);
                thumbnail.LoadThumbnailAsync().ContinueWithoutWaiting();
            }
        }

        protected override async Task<List<string>> GetFileNamesAsync()
        {
            // Query to retreive colorings ordered by date modified.
            QueryOptions queryOptions = new QueryOptions(CommonFolderQuery.DefaultQuery);

            queryOptions.SortOrder.Clear();

            SortEntry se = new SortEntry
            {
                PropertyName = "System.DateModified",
                AscendingOrder = false
            };
            queryOptions.SortOrder.Add(se);

            var assetsDir = await Tools.GetAssetsFolderAsync();
            var libraryImagesDir = await Tools.GetSubDirectoryAsync(assetsDir, "LibraryImages");
            StorageFolderQueryResult queryResult = libraryImagesDir.CreateFolderQueryWithOptions(queryOptions);

            var libraryImages = await queryResult.GetFoldersAsync();

            return libraryImages.Select(image => image.Name).ToList();
        }

        protected override void NonSelectionMode_OnItemClick(object sender, ItemClickEventArgs e) =>
            (e.ClickedItem as Thumbnail)?.OnItemClick();
    }
}
