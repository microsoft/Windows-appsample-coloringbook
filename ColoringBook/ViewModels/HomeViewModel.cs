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
using System.Threading.Tasks;
using ColoringBook.Common;
using ColoringBook.Views;

namespace ColoringBook.ViewModels
{
    public class HomeViewModel : Observable
    {
        public HomeViewModel(PivotItemViewModel pivotItemViewModel)
        {
            PivotItemViewModel = pivotItemViewModel;
            PivotItemViewModel.UpdateHomeCommandBar += UpdateCommandBar;
            PivotItemViewModel.UpdateHomePivotIndex += UpdateHomePivotIndex;
        }

        private PivotItemViewModel PivotItemViewModel { get; }

        public int PivotSelectedIndex
        {
            get => _pivotSelectedIndex;
            set
            {
                Set(ref _pivotSelectedIndex, value);
                DisableMultiSelect();
            }
        }

        private int _pivotSelectedIndex;

        public void UpdateIsSelectButtonEnabled() => OnPropertyChanged(nameof(IsSelectButtonEnabled));

        public bool IsSelectButtonEnabled => (PivotSelectedIndex == (int)PageIndexEnum.MyArtworkIndex) &&
                                              PivotItemViewModel?.ThumbnailsSource?.Count > 0;

        public bool IsMultiSelectEnabled
        {
            get => _isMultiSelectEnabled;
            set
            {
                Set(ref _isMultiSelectEnabled, value);
                OnPropertyChanged(nameof(IsDeleteButtonEnabled));
            }
        }

        private bool _isMultiSelectEnabled;

        public void EnableMultiSelect() => IsMultiSelectEnabled = true;

        public void DisableMultiSelect() => IsMultiSelectEnabled = false;

        private void UpdateCommandBar(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(IsDeleteButtonEnabled));
            OnPropertyChanged(nameof(IsSelectButtonEnabled));
        }

        public async void OnResetColorPalette() => await ColorPaletteViewModel.DeleteColorFileAsync();

        private void UpdateHomePivotIndex(object sender, PageIndexChangedEventArgs e) =>
            PivotSelectedIndex = (int)e.NewIndex;

        public bool IsDeleteButtonEnabled => PivotItemViewModel?.SelectedThumbnails?.Count > 0;

        public async Task OnDeleteSelectedItems()
        {
            await PivotItemViewModel.DeleteSelectedItemsAsync();
            IsMultiSelectEnabled = false;
        }
    }
}
