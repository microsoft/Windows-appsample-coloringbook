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
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Xaml.Controls;

namespace ColoringBook.Views
{
    public sealed class MyArtworkPivotItem : ColoringBookPivotItem
    {
        public MyArtworkPivotItem()
            : base()
        {
            Header = Tools.GetResourceString("MyArtworkPivotItem/Header");
            LoadThumbnailGroupAsync().ContinueWithoutWaiting();
        }

        public async override Task LoadThumbnailGroupAsync()
        {
            var previousColorings = await GetFileNamesAsync();

            var bFirst = true;
            foreach (var coloringName in previousColorings)
            {
                var thumbnail = new MyArtworkThumbnail(coloringName, bFirst);
                ViewModel.ThumbnailsSource.Add(thumbnail);
                thumbnail.LoadThumbnailAsync().ContinueWithoutWaiting();
                bFirst = false;
            }
        }

        protected override async Task<List<string>> GetFileNamesAsync()
        {
            var coloringDirectory = await Tools.GetColoringsDirectoryAsync();

            // Query to retreive colorings ordered by date modified.
            var queryOptions = new QueryOptions(CommonFolderQuery.DefaultQuery);

            queryOptions.SortOrder.Clear();

            var se = new SortEntry
            {
                PropertyName = "System.DateModified",
                AscendingOrder = false
            };
            queryOptions.SortOrder.Add(se);

            var queryResult = coloringDirectory.CreateFolderQueryWithOptions(queryOptions);

            var coloringFolders = new List<StorageFolder>();

            foreach (var folder in await queryResult.GetFoldersAsync())
            {
                if ((await folder.GetFilesAsync()).Any(file => file.Name.EndsWith(".thumbnail")))
                {
                    coloringFolders.Add(folder);
                }
            }

            // Return a list of the coloring names.
            return coloringFolders.Select(coloring => coloring.Name).ToList();
        }

        protected override void NonSelectionMode_OnItemClick(object sender, ItemClickEventArgs e) =>
            (e.ClickedItem as Thumbnail)?.OnItemClick();
    }
}
