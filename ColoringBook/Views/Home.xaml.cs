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
using ColoringBook.Common;
using ColoringBook.ViewModels;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace ColoringBook.Views
{
    public enum PageIndexEnum
    {
        MyArtworkIndex = 0,
        LibraryIndex = 1
    }

    public sealed partial class Home : Page
    {
        public HomeViewModel ViewModel { get; }

        public Home()
        {
            InitializeComponent();
            ViewModel = new HomeViewModel(MyArtworkPivotItem.ViewModel);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var currentView = SystemNavigationManager.GetForCurrentView();
            currentView.AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;

            // Set pivot to specified pivot, otherwise set to default.
            // Default is MyArtwork if artworks exists, otherwise Library.
            if (e.Parameter != null)
            {
                ViewModel.PivotSelectedIndex = Equals(e.Parameter, typeof(LibraryPivotItem)) ?
                    (int)PageIndexEnum.LibraryIndex : (int)PageIndexEnum.MyArtworkIndex;
            }
            else
            {
                var coloringsDirectory = await Tools.GetColoringsDirectoryAsync();
                var folders = await coloringsDirectory.GetFoldersAsync();
                ViewModel.PivotSelectedIndex = folders.Count == 0 ? (int)PageIndexEnum.LibraryIndex : (int)PageIndexEnum.MyArtworkIndex;
            }

            base.OnNavigatedTo(e);
        }

        // Handle pointer events to disable panning behavior of ScrollViewer
        // in the Pivot control and support both touch inking and Pivot pattern.
        // We maintain default Pivot behavior in the Pivot header.
        // 48 pixels is the default height of the Pivot header.
        private void ScrollViewer_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint((ScrollViewer)sender).Position.Y > 48)
            {
                (sender as ScrollViewer).HorizontalScrollMode = ScrollMode.Disabled;
            }
            else
            {
                (sender as ScrollViewer).HorizontalScrollMode = ScrollMode.Enabled;
            }
        }

        // Handle pointer events to re-enable panning behavior of ScrollViewer
        // in the Pivot control.
        private void ScrollViewer_PointerExited(object sender, PointerRoutedEventArgs e) =>
            (sender as ScrollViewer).HorizontalScrollMode = ScrollMode.Enabled;

        public async void OnDeleteSelectedItems()
        {
            var confirmDeleteDialog = new ContentDialog
            {
                Title = Tools.GetResourceString("DeleteMultipleDialog/Title"),
                Content = Tools.GetResourceString("DeleteMultipleDialog/Content"),
                PrimaryButtonText = Tools.GetResourceString("DeleteMultipleDialog/OkText"),
                SecondaryButtonText = Tools.GetResourceString("DeleteMultipleDialog/CancelText")
            };

            if (await confirmDeleteDialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var deletingDialog = new ColoringBookLoadingDialog(Tools.GetResourceString("Dialog/Deleting"));
                deletingDialog.ShowAsync().AsTask().ContinueWithoutWaiting();
                await ViewModel.OnDeleteSelectedItems();
                deletingDialog.Hide();
            }
        }
    }
}