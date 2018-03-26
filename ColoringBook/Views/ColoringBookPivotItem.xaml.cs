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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ColoringBook.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace ColoringBook.Views
{
    public abstract partial class ColoringBookPivotItem : PivotItem, INotifyPropertyChanged
    {
        public PivotItemViewModel ViewModel { get; }

        protected ColoringBookPivotItem()
        {
            InitializeComponent();

            var currentPageIndex = (this is MyArtworkPivotItem)
                ? PageIndexEnum.MyArtworkIndex
                : PageIndexEnum.LibraryIndex;

            ViewModel = new PivotItemViewModel(currentPageIndex);
        }

        public bool IsMultiSelectEnabled
        {
            get => _isMultiSelectEnabled;
            set
            {
                _isMultiSelectEnabled = value;
                UpdateThumbnailMultipleSelectionCheckboxes();
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectionMode));
            }
        }

        private bool _isMultiSelectEnabled;

        private ListViewSelectionMode SelectionMode =>
            IsMultiSelectEnabled ? ListViewSelectionMode.Multiple : ListViewSelectionMode.None;

        // Due to a bug within GridView we use the following workaround
        // to visually update the selected state of each of the thumbnails.
        private void UpdateThumbnailMultipleSelectionCheckboxes()
        {
            if (ThumbnailGridView.Items == null)
            {
                return;
            }

            foreach (var item in ThumbnailGridView.Items)
            {
                if (!(ThumbnailGridView.ContainerFromItem(item) is GridViewItem container))
                {
                    continue;
                }

                VisualStateManager.GoToState(container,
                    IsMultiSelectEnabled ? "MultiSelectEnabled" : "MultiSelectDisabled", true);
            }
        }

        public abstract Task LoadThumbnailGroupAsync();

        protected abstract Task<List<string>> GetFileNamesAsync();

        protected abstract void NonSelectionMode_OnItemClick(object sender, ItemClickEventArgs e);

        private void Hyperlink_OnClick(Hyperlink sender, HyperlinkClickEventArgs args) =>
            ViewModel.OnUpdateHomePivotIndex(PageIndexEnum.LibraryIndex);

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
